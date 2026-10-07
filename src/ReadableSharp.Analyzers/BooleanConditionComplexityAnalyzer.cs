using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadableSharp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BooleanConditionComplexityAnalyzer : DiagnosticAnalyzer
{
    private const int DefaultMaxComplexity = 5;
    private const string OptionKey = "readablesharp_rsharp1002.max_complexity";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.BooleanConditionTooComplex,
        "Boolean condition is too complex",
        "Boolean condition complexity {0} exceeds the configured limit {1}",
        "Readability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Name meaningful predicates instead of forcing readers to evaluate many boolean facts at once.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeCondition,
            SyntaxKind.IfStatement,
            SyntaxKind.WhileStatement,
            SyntaxKind.DoStatement,
            SyntaxKind.ForStatement,
            SyntaxKind.ConditionalExpression);
    }

    private static void AnalyzeCondition(SyntaxNodeAnalysisContext context)
    {
        var condition = context.Node switch
        {
            IfStatementSyntax statement => statement.Condition,
            WhileStatementSyntax statement => statement.Condition,
            DoStatementSyntax statement => statement.Condition,
            ForStatementSyntax statement => statement.Condition,
            ConditionalExpressionSyntax expression => expression.Condition,
            _ => null,
        };

        if (condition is null)
        {
            return;
        }

        var complexity = Measure(condition);
        var limit = AnalyzerOptionReader.GetInt32(context.Options, condition.SyntaxTree, OptionKey, DefaultMaxComplexity);

        if (complexity > limit)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, condition.GetLocation(), complexity, limit));
        }
    }

    private static int Measure(ExpressionSyntax condition)
    {
        var walker = new BooleanComplexityWalker();
        walker.Visit(condition);
        return walker.HasBooleanStructure ? walker.Score + 1 : 1;
    }

    private sealed class BooleanComplexityWalker : CSharpSyntaxWalker
    {
        public int Score { get; private set; }

        public bool HasBooleanStructure { get; private set; }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (IsLogical(node))
            {
                HasBooleanStructure = true;
                Score++;

                if (node.Parent is BinaryExpressionSyntax parent
                    && IsLogical(parent)
                    && parent.Kind() != node.Kind())
                {
                    Score++;
                }
            }

            base.VisitBinaryExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.LogicalNotExpression))
            {
                HasBooleanStructure = true;
                Score++;
            }

            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            if (node.Parent is BinaryExpressionSyntax parent
                && IsLogical(parent)
                && node.Expression.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().Any(IsLogical))
            {
                Score++;
            }

            base.VisitParenthesizedExpression(node);
        }

        private static bool IsLogical(BinaryExpressionSyntax expression) =>
            expression.IsKind(SyntaxKind.LogicalAndExpression)
            || expression.IsKind(SyntaxKind.LogicalOrExpression);
    }
}
