using ReadableSharp.Analyzers;
using Xunit;

namespace ReadableSharp.Tests;

public sealed class ExpressionNestingAnalyzerTests
{
    [Fact]
    public async Task ReportsDeeplyNestedExpression()
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

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new ExpressionNestingAnalyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("RSHARP1001", diagnostic.Id);
        Assert.Contains("depth 5", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DoesNotReportOrdinaryFluentChain()
    {
        const string source = """
            using System.Linq;
            class C
            {
                int[] M(int[] values) => values.Where(x => x > 0).Select(x => x + 1).ToArray();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new ExpressionNestingAnalyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task HonorsConfiguredDepth()
    {
        const string source = """
            class C
            {
                int M(int value) => A(B(C1(value)));
                int A(int x) => x;
                int B(int x) => x;
                int C1(int x) => x;
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(
            source,
            new ExpressionNestingAnalyzer(),
            new Dictionary<string, string> { ["readablesharp_rsharp1001.max_depth"] = "2" });

        Assert.Single(diagnostics);
    }
}
