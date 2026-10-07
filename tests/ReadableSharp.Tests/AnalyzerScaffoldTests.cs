using ReadableSharp.Analyzers;
using Xunit;

namespace ReadableSharp.Tests;

public sealed class AnalyzerScaffoldTests
{
    [Fact]
    public void SupportedDiagnostics_ContainsAllMvpRules()
    {
        var analyzer = new ReadableSharpAnalyzer();

        var ids = analyzer.SupportedDiagnostics.Select(diagnostic => diagnostic.Id).ToArray();

        Assert.Equal(
            new[]
            {
                "RSHARP1001",
                "RSHARP1002",
                "RSHARP1003",
                "RSHARP1004",
                "RSHARP1005",
                "RSHARP1006",
                "RSHARP1007",
            },
            ids);
    }
}
