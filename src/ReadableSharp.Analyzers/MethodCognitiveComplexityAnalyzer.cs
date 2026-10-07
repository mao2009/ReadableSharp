using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadableSharp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodCognitiveComplexityAnalyzer : DiagnosticAnalyzer
{
    private const int DefaultMaxComplexity = 15;
    private const string OptionKey = "readablesharp_rsharp1006.max_complexity";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MethodCognitiveComplexityTooHigh,
        "Method cognitive complexity is too high",
        "Method cognitive complexity {0} exceeds the configured limit {1}",
        "Readability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reduce nested control flow and name complex sub-decisions so the method can be understood incrementally.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        SyntaxNode? root = method.Body ?? (SyntaxNode?)method.ExpressionBody?.Expression;

        if (root is null)
        {
            return;
        }

        var calculator = new CognitiveComplexityWalker();
        calculator.Visit(root);

        var limit = AnalyzerOptionReader.GetInt32(context.Options, method.SyntaxTree, OptionKey, DefaultMaxComplexity);

        if (calculator.Score > limit)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                method.Identifier.GetLocation(),
                calculator.Score,
                limit));
        }
    }

    private sealed class CognitiveComplexityWalker : CSharpSyntaxWalker
    {
        private int _nesting;

        public int Score { get; private set; }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            var isGuardClause = _nesting == 0 && node.Else is null && IsExit(node.Statement);

            Score += 1 + (isGuardClause ? 0 : _nesting);
            Visit(node.Condition);

            if (isGuardClause)
            {
                Visit(node.Statement);
                return;
            }

            VisitNested(node.Statement);

            if (node.Else is null)
            {
                return;
            }

            if (node.Else.Statement is IfStatementSyntax elseIf)
            {
                Visit(elseIf);
            }
            else
            {
                Score++;
                VisitNested(node.Else.Statement);
            }
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            AddControlFlow(node.Condition, node.Statement);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            Score += 1 + _nesting;
            VisitNested(node.Statement);
            Visit(node.Condition);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            Score += 1 + _nesting;

            foreach (var initializer in node.Initializers)
            {
                Visit(initializer);
            }

            if (node.Condition is not null)
            {
                Visit(node.Condition);
            }

            foreach (var incrementor in node.Incrementors)
            {
                Visit(incrementor);
            }

            VisitNested(node.Statement);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            Score += 1 + _nesting;
            Visit(node.Expression);
            VisitNested(node.Statement);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            Score += 1 + _nesting;
            Visit(node.Expression);

            _nesting++;

            for (var index = 0; index < node.Sections.Count; index++)
            {
                if (index > 0)
                {
                    Score++;
                }

                Visit(node.Sections[index]);
            }

            _nesting--;
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            Score += 1 + _nesting;

            if (node.Filter is not null)
            {
                Visit(node.Filter.FilterExpression);
            }

            VisitNested(node.Block);
        }

        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            Score += 1 + _nesting;
            Visit(node.Condition);

            _nesting++;
            Visit(node.WhenTrue);
            Visit(node.WhenFalse);
            _nesting--;
        }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            Score++;
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            Score++;
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            Score++;
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            Score++;
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (IsLogical(node)
                && (node.Parent is not BinaryExpressionSyntax parent
                    || !IsLogical(parent)
                    || parent.Kind() != node.Kind()))
            {
                Score++;
            }

            base.VisitBinaryExpression(node);
        }

        private void AddControlFlow(ExpressionSyntax condition, StatementSyntax statement)
        {
            Score += 1 + _nesting;
            Visit(condition);
            VisitNested(statement);
        }

        private void VisitNested(SyntaxNode node)
        {
            _nesting++;
            Visit(node);
            _nesting--;
        }

        private static bool IsLogical(BinaryExpressionSyntax expression) =>
            expression.IsKind(SyntaxKind.LogicalAndExpression)
            || expression.IsKind(SyntaxKind.LogicalOrExpression);

        private static bool IsExit(StatementSyntax statement) =>
            statement is ReturnStatementSyntax
            or ThrowStatementSyntax
            or BreakStatementSyntax
            or ContinueStatementSyntax
            || statement is BlockSyntax block && block.Statements.LastOrDefault() is ReturnStatementSyntax
                or ThrowStatementSyntax
                or BreakStatementSyntax
                or ContinueStatementSyntax;
    }
}
