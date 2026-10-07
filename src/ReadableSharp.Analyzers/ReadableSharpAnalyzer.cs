using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadableSharp.Analyzers;

/// <summary>
/// Declares the complete MVP diagnostic surface for release tracking.
/// Rule implementations live in independent analyzers so they can evolve and be tested separately.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReadableSharpAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Readability";

    public static readonly DiagnosticDescriptor ExpressionNestingTooDeep = Create(
        DiagnosticIds.ExpressionNestingTooDeep,
        "Expression nesting is too deep",
        "Expression nesting depth {0} exceeds the configured limit {1}");

    public static readonly DiagnosticDescriptor BooleanConditionTooComplex = Create(
        DiagnosticIds.BooleanConditionTooComplex,
        "Boolean condition is too complex",
        "Boolean condition complexity {0} exceeds the configured limit {1}");

    public static readonly DiagnosticDescriptor LinqChainTooComplex = Create(
        DiagnosticIds.LinqChainTooComplex,
        "LINQ chain is too complex",
        "LINQ chain complexity {0} exceeds the configured limit {1}");

    public static readonly DiagnosticDescriptor NestedConditionalOperator = Create(
        DiagnosticIds.NestedConditionalOperator,
        "Nested conditional operator reduces readability",
        "Extract the nested conditional operator into a named intermediate value or method");

    public static readonly DiagnosticDescriptor LambdaNestingTooDeep = Create(
        DiagnosticIds.LambdaNestingTooDeep,
        "Lambda nesting is too deep",
        "Lambda nesting depth {0} exceeds the configured limit {1}");

    public static readonly DiagnosticDescriptor MethodCognitiveComplexityTooHigh = Create(
        DiagnosticIds.MethodCognitiveComplexityTooHigh,
        "Method cognitive complexity is too high",
        "Method cognitive complexity {0} exceeds the configured limit {1}");

    public static readonly DiagnosticDescriptor ComplexExpressionShouldBeExtracted = Create(
        DiagnosticIds.ComplexExpressionShouldBeExtracted,
        "Complex expression should be named or extracted",
        "Expression complexity {0} exceeds the configured limit {1}; contributors: {2}");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            ExpressionNestingTooDeep,
            BooleanConditionTooComplex,
            LinqChainTooComplex,
            NestedConditionalOperator,
            LambdaNestingTooDeep,
            MethodCognitiveComplexityTooHigh,
            ComplexExpressionShouldBeExtracted);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
    }

    private static DiagnosticDescriptor Create(string id, string title, string message) =>
        new(
            id,
            title,
            message,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "ReadableSharp detects structurally difficult-to-read C# and suggests reducing cognitive load.");
}
