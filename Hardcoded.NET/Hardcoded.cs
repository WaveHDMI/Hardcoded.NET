using Hardcoded.NET.SourceGenerators;
using Microsoft.CodeAnalysis;

namespace Hardcoded.NET;

[Generator]
public class Hardcoded : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        SqlSourceGenerator.Initialize(context);
	}
}
