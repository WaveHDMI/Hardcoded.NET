using Hardcoded.NET.Model.Reporting;
using Microsoft.CodeAnalysis;

namespace Hardcoded.NET.Common.Reporting;

internal static class DiagnosticDescriptors
{
    private const string Category = "Hardcoded.Gen";

    internal static readonly DiagnosticDescriptor UnhandledError = new(
		id: "HC0001",
		title: "Unhandled Error",
		messageFormat: "Hardcoded.NET failed to parse '{0}'. Error: {1}.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ParseError = new(
		id: "HC0002",
		title: "Parsing Failed",
		messageFormat: "Hardcoded.NET failed to parse '{0}'. Error: {1}.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	internal static readonly DiagnosticDescriptor InvalidNamespace = new(
		id: "HC0003",
		title: "Invalid Namespace",
		messageFormat: "Invalid namespace '{0}' in file '{1}'. Namespaces must be valid C# identifiers separated by dots.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	internal static readonly DiagnosticDescriptor InvalidClassName = new(
		id: "HC0004",
		title: "Invalid Class Name",
		messageFormat: "Invalid class name '{0}' in file '{1}'. Class names must be valid C# identifiers.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	internal static readonly DiagnosticDescriptor InvalidQueryName = new(
		id: "HC0005",
		title: "Invalid Query Name",
		messageFormat: "Invalid query name '{0}' in file '{1}'. Query names must be valid C# identifiers.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	extension(SourceProductionContext sourceProductionContext)
	{
		internal void ReportProblem(DiagnosticDescriptor descriptor, params object?[]? messageArgs)
		{
			sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, messageArgs));
		}

		internal void HandleException(Exception ex, string? filePath = null)
		{
			var fileName = filePath ?? "unknown file";
			if (ex is SourceParseException parseEx)
			{
				fileName = parseEx.FilePath;
			}

			// Report it to the compiler
			sourceProductionContext.ReportProblem(
				ex is SourceParseException ? ParseError : UnhandledError,
				new object?[] { Path.GetFileName(fileName), ex.Message });
		}
	}
}
