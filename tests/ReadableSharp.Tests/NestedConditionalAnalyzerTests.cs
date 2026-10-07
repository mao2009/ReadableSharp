using ReadableSharp.Analyzers;
using Xunit;

namespace ReadableSharp.Tests;

public sealed class NestedConditionalAnalyzerTests
{
    [Fact]
    public async Task DoesNotReportSingleConditional()
    {
        const string source = """
            class C
            {
                string M(bool value) => value ? "yes" : "no";
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new NestedConditionalAnalyzer());

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("a ? (b ? 1 : 2) : 3")]
    [InlineData("a ? 1 : (b ? 2 : 3)")]
    [InlineData("(a ? b : c) ? 1 : 2")]
    public async Task ReportsNestedConditionalOnce(string expression)
    {
        var source = $$"""
            class C
            {
                int M(bool a, bool b, bool c) => {{expression}};
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new NestedConditionalAnalyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("RSHARP1004", diagnostic.Id);
    }
}
