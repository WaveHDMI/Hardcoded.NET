namespace Hardcoded.NET.Common;

internal static class NameValidator
{
    private const char Underscore = '_';
    private const char NamespaceSeparator = '.';

	internal static bool IsInvalidCSharpName(string identifier)
	{
		if (string.IsNullOrWhiteSpace(identifier))
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
