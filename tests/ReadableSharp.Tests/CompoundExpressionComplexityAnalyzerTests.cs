using ReadableSharp.Analyzers;
using Xunit;

namespace ReadableSharp.Tests;

public sealed class CompoundExpressionComplexityAnalyzerTests
{
    [Fact]
    public async Task ReportsExpressionWithMultipleComplexitySources()
    {
        const string source = """
            using System.Linq;
            class Item
            {
                public bool Active { get; set; }
                public string? Name { get; set; }
                public int Score { get; set; }
            }

            class C
            {
                string[] M(Item[] items) =>
                    items
                        .Where(x => x.Active && x.Score > 0)
                        .Select(x => x.Name is null ? "" : (x.Name ?? ""))
                        .OrderBy(x => x)
                        .ToArray();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(
            source,
            new CompoundExpressionComplexityAnalyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("RSHARP1007", diagnostic.Id);
        Assert.Contains("contributors:", diagnostic.GetMessage());
        Assert.Contains("multi-stage LINQ", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DoesNotReportOrdinaryIdiomaticPipeline()
    {
        const string source = """
            using System.Linq;
            class Item
            {
                public bool Active { get; set; }
                public string Name { get; set; } = "";
            }

            class C
            {
                string[] M(Item[] items) =>
                    items.Where(x => x.Active).Select(x => x.Name).ToArray();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(
            source,
            new CompoundExpressionComplexityAnalyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotDuplicateSingleSourceOfComplexity()
    {
        const string source = """
            class C
            {
                int M(int value) => A(B(C1(D(E(value)))));
                int A(int x) => x;
                int B(int x) => x;
                int C1(int x) => x;
                int D(int x) => x;
                int E(int x) => x;
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(
            source,
            new CompoundExpressionComplexityAnalyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task HonorsConfiguredLimit()
    {
        const string source = """
            using System.Linq;
            class C
            {
                string[] M(int[] values) =>
                    values.Where(x => x > 0).Select(x => x.ToString()).ToArray();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(
            source,
            new CompoundExpressionComplexityAnalyzer(),
            new Dictionary<string, string>
            {
                ["readablesharp_rsharp1007.max_complexity"] = "6",
            });

        Assert.Single(diagnostics);
    }
}
