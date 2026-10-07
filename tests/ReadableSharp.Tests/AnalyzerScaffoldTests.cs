using ReadableSharp.Analyzers;
using Xunit;

namespace ReadableSharp.Tests;

public sealed class AnalyzerScaffoldTests
{
    [Fact]
    public void DiagnosticIds_AreStable()
    {
        Assert.Equal("RSHARP1001", DiagnosticIds.ExpressionNestingTooDeep);
        Assert.Equal("RSHARP1002", DiagnosticIds.BooleanConditionTooComplex);
        Assert.Equal("RSHARP1003", DiagnosticIds.LinqChainTooComplex);
        Assert.Equal("RSHARP1004", DiagnosticIds.NestedConditionalOperator);
        Assert.Equal("RSHARP1005", DiagnosticIds.LambdaNestingTooDeep);
        Assert.Equal("RSHARP1006", DiagnosticIds.MethodCognitiveComplexityTooHigh);
        Assert.Equal("RSHARP1007", DiagnosticIds.ComplexExpressionShouldBeExtracted);
    }
}
