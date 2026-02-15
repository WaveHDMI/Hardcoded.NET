namespace Hardcoded.NET.Common;

internal static class NameValidator
{
    private const char Underscore = '_';
    private const char NamespaceSeparator = '.';

    private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while"
    };

	internal static bool IsInvalidCSharpName(string identifier)
	{
		if (string.IsNullOrWhiteSpace(identifier))
		{
			return true;
		}

        // Check if it is a reserved keyword
        if (CSharpKeywords.Contains(identifier))
        {
            return true;
        }

		// Check first character (must be letter or underscore)
		if (!char.IsLetter(identifier[0]) && identifier[0] != Underscore)
		{
			return true;
		}

		// Check remaining characters (letters, digits, underscores)
		return identifier.Skip(1).Any(character => !char.IsLetterOrDigit(character) && character != Underscore);
	}

	internal static bool IsInvalidNamespace(string namespaceName)
	{
		if (string.IsNullOrWhiteSpace(namespaceName))
		{
			return true;
		}

		// Namespace can contain dots (e.g., "Hardcoded.Sql.Queries")
		var parts = namespaceName.Split(NamespaceSeparator);
		return parts.Any(part => part == null || IsInvalidCSharpName(part));
	}
}
