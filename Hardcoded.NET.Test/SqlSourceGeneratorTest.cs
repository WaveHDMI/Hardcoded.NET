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
-- @query TestQuery1
-- Test comment

SELECT *
FROM [dbo].[Test]
WHERE [Id] = @Id
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
WHERE [Id] = @Id";

}

""", Encoding.UTF8)));

		await context.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
    public async Task TestWithStart()
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
-- @query TestQuery1
-- Test comment
--
DECLARE @ParamVar INT = 0
-- New line

-- @start

DECLARE @Id INT = 1

SELECT *
FROM [dbo].[Test]
WHERE [Id] = @Id AND [CodeInt] = @ParamVar
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
    /// 
    /// New line
    /// </summary>
    internal const string TestQuery1 = @"DECLARE @Id INT = 1

SELECT *
FROM [dbo].[Test]
WHERE [Id] = @Id AND [CodeInt] = @ParamVar";

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
-- @query int
SELECT 1
"""
            ));

        // Expect diagnostics for invalid class name "class"
        context.ExpectedDiagnostics.Add(new DiagnosticResult("HC0004", DiagnosticSeverity.Warning).WithArguments("class", "InvalidQueries.sql"));

        await context.RunAsync(TestContext.Current.CancellationToken);
    }
}
