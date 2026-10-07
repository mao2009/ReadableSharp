using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadableSharp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CompoundExpressionComplexityAnalyzer : DiagnosticAnalyzer
{
    private const int DefaultMaxComplexity = 8;
    private const string OptionKey = "readablesharp_rsharp1007.max_complexity";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ComplexExpressionShouldBeExtracted,
        "Complex expression should be named or extracted",
        "Expression complexity {0} exceeds the configured limit {1}; contributors: {2}",
        "Readability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Name meaningful intermediate concepts when one expression mixes too many forms of work.");

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

        var metrics = Measure(expression, context.SemanticModel);

        // RSHARP1007 is a synthesis rule. One isolated source of complexity is
        // better handled by the dedicated RSHARP1001-RSHARP1006 rules.
        if (metrics.Contributors.Count < 2)
        {
            return;
        }

        var limit = AnalyzerOptionReader.GetInt32(context.Options, expression.SyntaxTree, OptionKey, DefaultMaxComplexity);

        if (metrics.Score > limit)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                expression.GetLocation(),
                metrics.Score,
                limit,
                string.Join(", ", metrics.Contributors)));
        }
    }

    private static Metrics Measure(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var contributors = new List<string>();
        var score = 0;

        var nesting = MeasureDepth(expression, 0);
        if (nesting >= 4)
        {
            score += nesting - 2;
            contributors.Add("deep nesting");
        }

        var conditionalCount = expression.DescendantNodesAndSelf().OfType<ConditionalExpressionSyntax>().Count();
        if (conditionalCount > 0)
        {
            score += conditionalCount * 2;
            contributors.Add("conditional flow");
        }

        var coalesceCount = expression.DescendantNodesAndSelf()
            .OfType<BinaryExpressionSyntax>()
            .Count(binary => binary.IsKind(SyntaxKind.CoalesceExpression));
        if (coalesceCount > 1)
        {
            score += coalesceCount;
            contributors.Add("null/coalesce chain");
        }

        var lambdaCount = expression.DescendantNodesAndSelf().OfType<AnonymousFunctionExpressionSyntax>().Count();
        if (lambdaCount > 0)
        {
            score += lambdaCount * 2;
            contributors.Add("inline lambdas");
        }

        var invocations = expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToArray();
        var linqCount = invocations.Count(invocation => IsLinq(invocation, semanticModel));
        if (linqCount >= 3)
        {
            score += linqCount;
            contributors.Add("multi-stage LINQ");
        }

        if (invocations.Length >= 4)
        {
            score += invocations.Length - 2;
            contributors.Add("many calls");
        }

        var operationKinds = CountOperationKinds(expression);
        if (operationKinds >= 3)
        {
            score += 2;
            contributors.Add("mixed operations");
        }

        return new Metrics(score, contributors);
    }

    private static int MeasureDepth(ExpressionSyntax expression, int parentDepth)
    {
        var currentDepth = parentDepth + (ContributesDepth(expression) ? 1 : 0);
        var maxDepth = currentDepth;

        foreach (var child in expression.ChildNodes().OfType<ExpressionSyntax>())
        {
            maxDepth = Math.Max(maxDepth, MeasureDepth(child, currentDepth));
        }

        return maxDepth;
    }

    private static int CountOperationKinds(ExpressionSyntax expression)
    {
        var hasInvocation = false;
        var hasBinary = false;
        var hasConditional = false;
        var hasCreation = false;
        var hasElementAccess = false;

        foreach (var node in expression.DescendantNodesAndSelf())
        {
            hasInvocation |= node is InvocationExpressionSyntax;
            hasBinary |= node is BinaryExpressionSyntax;
            hasConditional |= node is ConditionalExpressionSyntax;
            hasCreation |= node is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax;
            hasElementAccess |= node is ElementAccessExpressionSyntax;
        }

        return (hasInvocation ? 1 : 0)
            + (hasBinary ? 1 : 0)
            + (hasConditional ? 1 : 0)
            + (hasCreation ? 1 : 0)
            + (hasElementAccess ? 1 : 0);
    }

    private static bool IsLinq(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        var definingMethod = method.ReducedFrom ?? method;
        return definingMethod.ContainingType?.ToDisplayString() is "System.Linq.Enumerable" or "System.Linq.Queryable";
    }

    private static bool ContributesDepth(ExpressionSyntax expression) =>
        expression is not IdentifierNameSyntax
        and not LiteralExpressionSyntax
        and not ThisExpressionSyntax
        and not BaseExpressionSyntax
        and not MemberAccessExpressionSyntax
        and not ParenthesizedExpressionSyntax;

    private sealed class Metrics
    {
        public Metrics(int score, IReadOnlyList<string> contributors)
        {
            Score = score;
            Contributors = contributors;
        }

        public int Score { get; }

        public IReadOnlyList<string> Contributors { get; }
    }
}
