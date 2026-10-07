namespace ReadableSharp.Analyzers;

public static class DiagnosticIds
{
    public const string ExpressionNestingTooDeep = "RSHARP1001";
    public const string BooleanConditionTooComplex = "RSHARP1002";
    public const string LinqChainTooComplex = "RSHARP1003";
    public const string NestedConditionalOperator = "RSHARP1004";
    public const string LambdaNestingTooDeep = "RSHARP1005";
    public const string MethodCognitiveComplexityTooHigh = "RSHARP1006";
    public const string ComplexExpressionShouldBeExtracted = "RSHARP1007";
}
