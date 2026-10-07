using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadableSharp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LambdaNestingAnalyzer : DiagnosticAnalyzer
{
    private const int DefaultMaxDepth = 1;
    private const string OptionKey = "readablesharp_rsharp1005.max_depth";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.LambdaNestingTooDeep,
        "Lambda nesting is too deep",
        "Lambda nesting depth {0} exceeds the configured limit {1}",
        "Readability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Nested anonymous functions require readers to track multiple parameter scopes and contexts simultaneously.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeLambda,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.AnonymousMethodExpression);
    }

    private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
    {
        if (context.Node.DescendantNodes().Any(IsAnonymousFunction))
        {
            return;
        }

        var depth = 1 + context.Node.Ancestors().Count(IsAnonymousFunction);
        var limit = AnalyzerOptionReader.GetInt32(context.Options, context.Node.SyntaxTree, OptionKey, DefaultMaxDepth);

        if (depth > limit)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), depth, limit));
        }
    }

    private static bool IsAnonymousFunction(SyntaxNode node) =>
        node is SimpleLambdaExpressionSyntax
        or ParenthesizedLambdaExpressionSyntax
        or AnonymousMethodExpressionSyntax;
}
