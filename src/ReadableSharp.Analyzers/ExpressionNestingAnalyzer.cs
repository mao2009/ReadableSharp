using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadableSharp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExpressionNestingAnalyzer : DiagnosticAnalyzer
{
    private const int DefaultMaxDepth = 4;
    private const string OptionKey = "readablesharp_rsharp1001.max_depth";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ExpressionNestingTooDeep,
        "Expression nesting is too deep",
        "Expression nesting depth {0} exceeds the configured limit {1}",
        "Readability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Extract meaningful intermediate values when nested expressions require too much context to understand.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeExpressionRoot,
            SyntaxKind.ReturnStatement,
            SyntaxKind.EqualsValueClause,
            SyntaxKind.ArrowExpressionClause,
            SyntaxKind.ExpressionStatement);
    }

    private static void AnalyzeExpressionRoot(SyntaxNodeAnalysisContext context)
    {
        var expression = context.Node switch
        {
            ReturnStatementSyntax statement => statement.Expression,
            EqualsValueClauseSyntax equalsValue => equalsValue.Value,
            ArrowExpressionClauseSyntax arrow => arrow.Expression,
            ExpressionStatementSyntax statement => statement.Expression,
            _ => null,
        };

        if (expression is null)
        {
            return;
        }

        var depth = MeasureDepth(expression, 0);
        var limit = AnalyzerOptionReader.GetInt32(context.Options, expression.SyntaxTree, OptionKey, DefaultMaxDepth);

        if (depth > limit)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation(), depth, limit));
        }
    }

    private static int MeasureDepth(ExpressionSyntax expression, int parentDepth)
    {
        var maxDepth = parentDepth;

        foreach (var candidate in expression.DescendantNodesAndSelf().OfType<ExpressionSyntax>())
        {
            var depth = parentDepth;
            SyntaxNode? current = candidate;

            while (current is not null)
            {
                if (current is AnonymousFunctionExpressionSyntax && current != expression)
                {
                    break;
                }

                if (current is ExpressionSyntax currentExpression && ContributesDepth(currentExpression))
                {
                    depth++;
                }

                if (current == expression)
                {
                    break;
                }

                current = current.Parent;
            }

            maxDepth = Math.Max(maxDepth, depth);
        }

        return maxDepth;
    }

    private static bool ContributesDepth(ExpressionSyntax expression) =>
        expression is not IdentifierNameSyntax
        and not LiteralExpressionSyntax
        and not ThisExpressionSyntax
        and not BaseExpressionSyntax
        and not MemberAccessExpressionSyntax
        and not ParenthesizedExpressionSyntax
        and not AnonymousFunctionExpressionSyntax;
}
