using ReadableSharp.Analyzers;
using Xunit;

namespace ReadableSharp.Tests;

public sealed class MethodCognitiveComplexityAnalyzerTests
{
    [Fact]
    public async Task ReportsPinnedNestedControlFlowScore()
    {
        const string source = """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        while (b)
                        {
                            if (a && b)
                            {
                                return;
                            }
                        }
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(
            source,
            new MethodCognitiveComplexityAnalyzer(),
            new Dictionary<string, string> { ["readablesharp_rsharp1006.max_complexity"] = "6" });

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("RSHARP1006", diagnostic.Id);
        Assert.Contains("complexity 7", diagnostic.GetMessage());
    }

    [Fact]
    public async Task GuardClausesAreCheaperThanEquivalentNesting()
    {
        const string guardSource = """
            class C
            {
                void M(bool ready, bool valid)
                {
                    if (!ready) return;
                    if (!valid) return;
                    Use();
                }

                void Use() { }
            }
            """;

        const string nestedSource = """
            class C
            {
                void M(bool ready, bool valid)
                {
                    if (ready)
                    {
                        if (valid)
                        {
                            Use();
                        }
                    }
                }

                void Use() { }
            }
            """;

        var options = new Dictionary<string, string>
        {
            ["readablesharp_rsharp1006.max_complexity"] = "2",
        };

        var guardDiagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(
            guardSource,
            new MethodCognitiveComplexityAnalyzer(),
            options);

        var nestedDiagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(
            nestedSource,
            new MethodCognitiveComplexityAnalyzer(),
            options);

        Assert.Empty(guardDiagnostics);
        Assert.Single(nestedDiagnostics);
    }

    [Fact]
    public async Task DefaultThresholdAllowsSmallMethod()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    if (value < 0)
                    {
                        return -value;
                    }

                    return value;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(source, new MethodCognitiveComplexityAnalyzer());

        Assert.Empty(diagnostics);
    }
}
