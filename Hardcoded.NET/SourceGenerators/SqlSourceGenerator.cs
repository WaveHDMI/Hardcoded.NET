using Hardcoded.NET.Common;
using Hardcoded.NET.Common.Reporting;
using Hardcoded.NET.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.RegularExpressions;

namespace Hardcoded.NET.SourceGenerators;

public static class SqlSourceGenerator
{
	private const string HardcodedTag = "-- @hardcoded ";

	// Regex patterns for parsing tags
	private static readonly Regex NamespaceRegex = new(@"--[ \t]*@namespace[ \t]+(\S+)\s", RegexOptions.Compiled);
	private static readonly Regex ClassRegex = new(@"--[ \t]*@class[ \t]+(\S+)\s", RegexOptions.Compiled);
	private static readonly Regex NameRegex = new(@"--[ \t]*@name[ \t]+(\S+)\s", RegexOptions.Compiled);

	public static void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Read and parse additional files for the project
		var resourceFiles = context.AdditionalTextsProvider
			.Where(static file => file.Path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
			.Select((file, cancellationToken) =>
			{
				try
				{
					var content = file.GetText(cancellationToken)?.ToString();
					if (content == null)
					{
						// Ignore files with no content or if unreadable
						return null;
					}

					return ParseFileMetadata(file.Path, content);
				}
				catch (Exception ex)
				{
					return new SqlFileMetadata(file.Path, []) { ParseException = ex };
				}
			})
			.Where(static metadata => metadata != null) // Filter out ignored files
			.Select(static (metadata, _) => metadata!) // Non-null assertion after filtering
			.Collect();

		context.RegisterSourceOutput(resourceFiles, (spc, metas) =>
		{
			try
			{
				foreach (var meta in metas)
				{
					if (meta.ParseException != null)
					{
						spc.HandleException(meta.ParseException, meta.OriginalFilePath);
					}
					else
					{
						GenerateClasses(spc, meta);
					}
				}
			}
			catch (Exception ex)
			{
				spc.HandleException(ex);
			}
		});
	}

	#region Parsing

	private static SqlFileMetadata? ParseFileMetadata(string filePath, string rawContent)
	{
		rawContent = rawContent.Trim();

		// Avoid checking files that are not marked as @hardcoded
		if (string.IsNullOrEmpty(rawContent))
		{
			return null;
		}

		var contentStartIndex = rawContent.IndexOf(HardcodedTag, StringComparison.Ordinal);
		var nextIndex = contentStartIndex + HardcodedTag.Length;
		if (contentStartIndex == -1 || nextIndex >= rawContent.Length)
		{
			return null;
		}

		var content = rawContent.Substring(nextIndex);
		var queries = ParseNamespaces(content);

		// Flatten the dictionary into SqlClassMetadata instances
		return new SqlFileMetadata(
			filePath,
			queries.SelectMany(namespaze => namespaze.Value.Select(clazz => new SqlClassMetadata(namespaze.Key, clazz.Key, clazz.Value))).ToList()
		);
	}

	private static Dictionary<string, Dictionary<string, Dictionary<string, SqlQueryMetadata>>> ParseNamespaces(string content)
	{
		// Namespace -> Class -> Constant/Field name -> Query string
		var queries = new Dictionary<string, Dictionary<string, Dictionary<string, SqlQueryMetadata>>>(StringComparer.OrdinalIgnoreCase);

		// Find all namespace matches
		// We will split each namespace block and parse the classes within
		var namespaceMatches = NamespaceRegex.Matches(content);

		for (int i = 0; i < namespaceMatches.Count; i++)
		{
			var namespaceMatch = namespaceMatches[i];
			var namespaceName = namespaceMatch.Groups[1].Value;

			// Determine the content range for this namespace
			var namespaceStartIndex = namespaceMatch.Index + namespaceMatch.Length;
			var namespaceEndIndex = (i < namespaceMatches.Count - 1) ? namespaceMatches[i + 1].Index : content.Length;

			var namespaceContent = content.Substring(namespaceStartIndex, namespaceEndIndex - namespaceStartIndex);

			// Get or create the namespace dictionary
			if (!queries.TryGetValue(namespaceName, out var classes))
			{
				classes = new Dictionary<string, Dictionary<string, SqlQueryMetadata>>(StringComparer.OrdinalIgnoreCase);
				queries[namespaceName] = classes;
			}

			// Parse classes within this namespace
			ParseClasses(namespaceContent, classes);
		}

		return queries;
	}

	private static void ParseClasses(string namespaceContent, Dictionary<string, Dictionary<string, SqlQueryMetadata>> classes)
	{
		// Find all class matches within the namespace content
		var classMatches = ClassRegex.Matches(namespaceContent);

		for (int i = 0; i < classMatches.Count; i++)
		{
			var classMatch = classMatches[i];
			var className = classMatch.Groups[1].Value;

			// Determine the content range for this class
			var classStartIndex = classMatch.Index + classMatch.Length;
			var classEndIndex = (i < classMatches.Count - 1) ? classMatches[i + 1].Index : namespaceContent.Length;

			var classContent = namespaceContent.Substring(classStartIndex, classEndIndex - classStartIndex);

			// Get or create the class dictionary
			if (!classes.TryGetValue(className, out var queries))
			{
				queries = new Dictionary<string, SqlQueryMetadata>(StringComparer.OrdinalIgnoreCase);
				classes[className] = queries;
			}

			// Parse query names within this class
			ParseQueries(classContent, queries);
		}
	}

	private static void ParseQueries(string classContent, Dictionary<string, SqlQueryMetadata> queries)
	{
		// Find all @name matches within the class content
		var nameMatches = NameRegex.Matches(classContent);

		for (var i = 0; i < nameMatches.Count; i++)
		{
			var nameMatch = nameMatches[i];
			var queryName = nameMatch.Groups[1].Value;

			// Determine the content range for this query
			var queryStartIndex = nameMatch.Index + nameMatch.Length;
			var queryEndIndex = (i < nameMatches.Count - 1) ? nameMatches[i + 1].Index : classContent.Length;

			var rawQueryBlock = classContent.Substring(queryStartIndex, queryEndIndex - queryStartIndex);
			
			// Separate the summary (comments) from the query content
			// We assume the summary is everything before the first non-comment line or the actual query
			// Simplified: The format is -- comments \n query
			
			var lines = rawQueryBlock.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var summaryBuilder = new StringBuilder();
			var queryBuilder = new StringBuilder();
			
			foreach (var line in lines)
			{
				var trimmedLine = line.Trim();
				if (trimmedLine.StartsWith("--"))
				{
					if (queryBuilder.Length == 0)
					{
						// It is part of the summary
						summaryBuilder.AppendLine(trimmedLine.Substring(2).Trim());
					}
					else
					{
						// It is a comment inside the query
						queryBuilder.AppendLine(line);
					}
				}
				else
				{
					queryBuilder.AppendLine(line);
				}
			}

			// Store the query
			queries[queryName] = new SqlQueryMetadata(queryBuilder.ToString().Trim(), summaryBuilder.ToString().Trim());
		}
	}

	#endregion

	#region Generation

	private static void GenerateClasses(SourceProductionContext sourceProductionContext, SqlFileMetadata metadata)
	{
		foreach (var (namespaceName, className, dictionary) in metadata.Classes)
		{
			// Validate namespace to prevent code injection
			if (NameValidator.IsInvalidNamespace(namespaceName))
			{
				sourceProductionContext.ReportProblem(
					DiagnosticDescriptors.InvalidNamespace,
					new object?[] { namespaceName, Path.GetFileName(metadata.OriginalFilePath) });
				continue;
			}

			// Validate identifiers to prevent code injection
			if (NameValidator.IsInvalidCSharpName(className))
			{
				sourceProductionContext.ReportProblem(
					DiagnosticDescriptors.InvalidClassName,
					new object?[] { className, Path.GetFileName(metadata.OriginalFilePath) });
				continue;
			}

			// Build the class code
			var codeBuilder = new StringBuilder();
			codeBuilder.AppendLine($"namespace {namespaceName};");
			codeBuilder.AppendLine();
			codeBuilder.AppendLine($"internal static partial class {className}");
			codeBuilder.AppendLine("{");

			foreach (var query in dictionary)
			{
				var queryName = query.Key;
				var queryMeta = query.Value;

				// Validate query name identifier
				if (NameValidator.IsInvalidCSharpName(queryName))
				{
					sourceProductionContext.ReportProblem(
						DiagnosticDescriptors.InvalidQueryName,
						new object?[] { className, Path.GetFileName(metadata.OriginalFilePath) });
					continue; // Skip this query
				}

				// Escape quotes for C# verbatim strings
				var safeSql = queryMeta.Content.Replace("\"", "\"\"");

				codeBuilder.AppendLine($"    /// <summary>");
				if (!string.IsNullOrWhiteSpace(queryMeta.Summary))
				{
					foreach (var line in queryMeta.Summary.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
					{
						codeBuilder.AppendLine($"    /// {line}");
					}
				}
				else
				{
					codeBuilder.AppendLine($"    /// Query from {Path.GetFileNameWithoutExtension(metadata.OriginalFilePath)}.sql");
				}
				codeBuilder.AppendLine("    /// </summary>");
				codeBuilder.AppendLine($"    internal const string {queryName} = @\"{safeSql}\";");
				codeBuilder.AppendLine();
			}

			codeBuilder.AppendLine("}");

			// Add source to compilation
			var uniqueFileName = $"{namespaceName}.{className}.g.cs";
			sourceProductionContext.AddSource(uniqueFileName, SourceText.From(codeBuilder.ToString(), Encoding.UTF8));
		}
	}

	#endregion
}
