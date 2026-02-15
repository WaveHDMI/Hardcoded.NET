namespace Hardcoded.NET.Model.Reporting;

public class SourceParseException : Exception
{
	public string FilePath { get; }

	public SourceParseException(string filePath, string message, Exception innerException) : base(message, innerException)
    {
		FilePath = filePath;
	}

	public SourceParseException(string filePath, string message) : base(message)
	{
		FilePath = filePath;
	}
}
