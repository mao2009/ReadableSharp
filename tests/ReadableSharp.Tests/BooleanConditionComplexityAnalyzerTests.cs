using ReadableSharp.Analyzers;
using Xunit;

namespace ReadableSharp.Tests;

public sealed class BooleanConditionComplexityAnalyzerTests
{
    [Fact]
    public async Task ReportsComplexMixedCondition()
    {
        const string source = """
            class C
            {
                bool M(bool a, bool b, bool c, bool d, bool e)
                {
                    if ((a && b) || (!c && d) || e)
                    {
                        return true;
                    }

                    return false;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new BooleanConditionComplexityAnalyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "RSHARP1002");
    }

    [Fact]
    public async Task DoesNotReportSimpleGuard()
    {
        const string source = """
            class C
            {
                bool M(bool ready, bool valid)
                {
                    if (ready && valid)
                    {
                        return true;
                    }

                    return false;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new BooleanConditionComplexityAnalyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task HonorsConfiguredLimit()
    {
        const string source = """
            class C
            {
                bool M(bool ready, bool valid)
                {
                    if (ready && valid)
                    {
                        return true;
                    }

                    return false;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(
            source,
            new BooleanConditionComplexityAnalyzer(),
            new Dictionary<string, string> { ["readablesharp_rsharp1002.max_complexity"] = "1" });

        Assert.Single(diagnostics);
    }
}
