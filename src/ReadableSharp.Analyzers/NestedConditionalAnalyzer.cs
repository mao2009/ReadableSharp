using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadableSharp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NestedConditionalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.NestedConditionalOperator,
        "Nested conditional operator reduces readability",
        "Extract the nested conditional operator into a named intermediate value, explicit branch, or method",
        "Readability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Nested conditional operators compress multiple control-flow decisions into one expression.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeConditional, SyntaxKind.ConditionalExpression);
    }

    private static void AnalyzeConditional(SyntaxNodeAnalysisContext context)
    {
        var conditional = (ConditionalExpressionSyntax)context.Node;

        if (conditional.Ancestors().OfType<ConditionalExpressionSyntax>().Any())
        {
            return;
        }

        if (conditional.DescendantNodes().OfType<ConditionalExpressionSyntax>().Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, conditional.GetLocation()));
        }
    }
}
