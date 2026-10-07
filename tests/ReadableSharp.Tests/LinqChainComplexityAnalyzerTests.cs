using ReadableSharp.Analyzers;
using Xunit;

namespace ReadableSharp.Tests;

public sealed class LinqChainComplexityAnalyzerTests
{
    [Fact]
    public async Task ReportsComplexMultiStageLinqPipeline()
    {
        const string source = """
            using System.Linq;
            class C
            {
                object M(int[] values) =>
                    values
                        .Where(x => x > 0)
                        .SelectMany(x => new[] { x, x + 1 })
                        .OrderBy(x => x)
                        .ThenBy(x => x)
                        .Select(x => x.ToString())
                        .ToDictionary(x => x, x => x.Length);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new LinqChainComplexityAnalyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("RSHARP1003", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportConciseIdiomaticPipeline()
    {
        const string source = """
            using System.Linq;
            class C
            {
                string[] M(int[] values) =>
                    values.Where(x => x > 0).Select(x => x.ToString()).ToArray();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new LinqChainComplexityAnalyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IgnoresSimilarlyNamedNonLinqMethods()
    {
        const string source = """
            class Pipeline
            {
                public Pipeline Where() => this;
                public Pipeline Select() => this;
                public Pipeline OrderBy() => this;
                public Pipeline ToList() => this;
            }

            class C
            {
                Pipeline M(Pipeline value) => value.Where().Select().OrderBy().ToList();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new LinqChainComplexityAnalyzer());

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
            new LinqChainComplexityAnalyzer(),
            new Dictionary<string, string> { ["readablesharp_rsharp1003.max_complexity"] = "4" });

        Assert.Single(diagnostics);
    }
}
