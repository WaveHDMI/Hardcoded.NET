namespace Hardcoded.NET.Model;

public sealed record SqlFileMetadata(string OriginalFilePath, List<SqlClassMetadata> Classes)
{
	public string OriginalFilePath { get; } = OriginalFilePath;
	public List<SqlClassMetadata> Classes { get; } = Classes;
	public Exception? ParseException { get; set; }
}

public sealed record SqlClassMetadata(string TargetNamespace, string TargetClass, Dictionary<string, string> Queries)
{
	public string TargetNamespace { get; } = TargetNamespace;
	public string TargetClass { get; } = TargetClass;
	public Dictionary<string, string> Queries { get; } = Queries;
}
