using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Xunit;

namespace Hardcoded.NET.Test;

public sealed class SqlSourceGeneratorTest
{
    [Fact]
    public async Task Test()
    {
        var context = new CSharpSourceGeneratorTest<Hardcoded, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = "" // No C# code needed
        };

        // Add SQL file as an additional file (this is what the source generator processes)
        context.TestState.AdditionalFiles.Add(("TestQueries.sql", """
-- @hardcoded 
-- @namespace Hardcoded.NET.Test
    
-- @class TestQueries
-- @name TestQuery1
-- Test comment
SELECT *
FROM [dbo].[Test]
WHERE [Id] = 1
"""
            ));

        // Expected generated source
        context.TestState.GeneratedSources.Add((
            typeof(Hardcoded),
            "Hardcoded.NET.Test.TestQueries.g.cs",
            SourceText.From("""
namespace Hardcoded.NET.Test;

internal static partial class TestQueries
{
    /// <summary>
    /// Test comment
    /// </summary>
    internal const string TestQuery1 = @"SELECT *
FROM [dbo].[Test]
WHERE [Id] = 1";

}

""", Encoding.UTF8)));

        await context.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TestInvalidIdentifier()
    {
        var context = new CSharpSourceGeneratorTest<Hardcoded, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = "" // No C# code needed
        };

        // Add SQL file as an additional file
        context.TestState.AdditionalFiles.Add(("InvalidQueries.sql", """
-- @hardcoded 
-- @namespace Hardcoded.NET.Test
    
-- @class class
-- @name int
SELECT 1
"""
            ));

        // Expect diagnostics for invalid class name "class"
        context.ExpectedDiagnostics.Add(new DiagnosticResult("HC0004", DiagnosticSeverity.Warning).WithArguments("class", "InvalidQueries.sql"));

        await context.RunAsync(TestContext.Current.CancellationToken);
    }
}
