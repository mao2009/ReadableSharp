using ReadableSharp.Analyzers;
using Xunit;

namespace ReadableSharp.Tests;

public sealed class LambdaNestingAnalyzerTests
{
    [Fact]
    public async Task ReportsNestedLambda()
    {
        const string source = """
            using System;
            class C
            {
                Func<int, Func<int, int>> M() => x => y => x + y;
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new LambdaNestingAnalyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("RSHARP1005", diagnostic.Id);
        Assert.Contains("depth 2", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DoesNotReportSiblingLambdas()
    {
        const string source = """
            using System;
            class C
            {
                void M()
                {
                    Func<int, int> first = x => x + 1;
                    Func<int, int> second = x => x + 2;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new LambdaNestingAnalyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task HonorsConfiguredDepth()
    {
        const string source = """
            using System;
            class C
            {
                Func<int, Func<int, Func<int, int>>> M() => x => y => z => x + y + z;
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(
            source,
            new LambdaNestingAnalyzer(),
            new Dictionary<string, string> { ["readablesharp_rsharp1005.max_depth"] = "2" });

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("depth 3", diagnostic.GetMessage());
    }
}
