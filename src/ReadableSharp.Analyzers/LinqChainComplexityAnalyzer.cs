using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadableSharp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LinqChainComplexityAnalyzer : DiagnosticAnalyzer
{
    private const int DefaultMaxComplexity = 7;
    private const string OptionKey = "readablesharp_rsharp1003.max_complexity";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.LinqChainTooComplex,
        "LINQ chain is too complex",
        "LINQ chain complexity {0} exceeds the configured limit {1}",
        "Readability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Split multi-stage LINQ pipelines when a reader must track too many transformations at once.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (IsReceiverOfInvocation(invocation))
        {
            return;
        }

        var chain = CollectLinqChain(invocation, context.SemanticModel);

        if (chain.Count == 0)
        {
            return;
        }

        var complexity = MeasureComplexity(chain, context.SemanticModel);
        var limit = AnalyzerOptionReader.GetInt32(context.Options, invocation.SyntaxTree, OptionKey, DefaultMaxComplexity);

        if (complexity > limit)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), complexity, limit));
        }
    }

    private static List<(InvocationExpressionSyntax Invocation, string MethodName)> CollectLinqChain(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        var result = new List<(InvocationExpressionSyntax, string)>();
        InvocationExpressionSyntax? current = invocation;

        while (current is not null && TryGetLinqMethodName(current, semanticModel, out var methodName))
        {
            result.Add((current, methodName));
            current = GetReceiverInvocation(current);
        }

        result.Reverse();
        return result;
    }

    private static int MeasureComplexity(
        IReadOnlyList<(InvocationExpressionSyntax Invocation, string MethodName)> chain,
        SemanticModel semanticModel)
    {
        var score = 0;
        string? previousCategory = null;

        foreach (var (invocation, methodName) in chain)
        {
            score++;

            var category = Categorize(methodName);
            if (previousCategory is not null && !string.Equals(previousCategory, category, StringComparison.Ordinal))
            {
                score++;
            }

            previousCategory = category;

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (argument.Expression is not AnonymousFunctionExpressionSyntax anonymousFunction)
                {
                    continue;
                }

                if (IsNonTrivial(anonymousFunction))
                {
                    score++;
                }

                if (ContainsNestedLinq(anonymousFunction, semanticModel))
                {
                    score += 2;
                }
            }
        }

        return score;
    }

    private static bool IsReceiverOfInvocation(InvocationExpressionSyntax invocation) =>
        invocation.Parent is MemberAccessExpressionSyntax memberAccess
        && memberAccess.Expression == invocation
        && memberAccess.Parent is InvocationExpressionSyntax;

    private static InvocationExpressionSyntax? GetReceiverInvocation(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Expression as InvocationExpressionSyntax
            : null;

    private static bool TryGetLinqMethodName(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out string methodName)
    {
        methodName = string.Empty;

        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        var definingMethod = method.ReducedFrom ?? method;
        var containingType = definingMethod.ContainingType?.ToDisplayString();

        if (containingType is not ("System.Linq.Enumerable" or "System.Linq.Queryable"))
        {
            return false;
        }

        methodName = definingMethod.Name;
        return true;
    }

    private static bool ContainsNestedLinq(
        AnonymousFunctionExpressionSyntax anonymousFunction,
        SemanticModel semanticModel) =>
        anonymousFunction.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => TryGetLinqMethodName(invocation, semanticModel, out _));

    private static bool IsNonTrivial(AnonymousFunctionExpressionSyntax anonymousFunction)
    {
        CSharpSyntaxNode? body = anonymousFunction switch
        {
            LambdaExpressionSyntax lambda => lambda.Body,
            AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.Block,
            _ => null,
        };

        return body switch
        {
            BlockSyntax block => block.Statements.Count > 1,
            ExpressionSyntax expression =>
                expression is ConditionalExpressionSyntax
                || expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Count() > 1
                || expression.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().Count() > 1,
            _ => false,
        };
    }

    private static string Categorize(string methodName) => methodName switch
    {
        "Where" or "OfType" => "filter",
        "Select" or "SelectMany" or "Cast" => "projection",
        "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" or "Reverse" => "ordering",
        "GroupBy" or "GroupJoin" or "Join" => "grouping",
        "ToArray" or "ToList" or "ToDictionary" or "ToLookup" => "materialization",
        "Any" or "All" or "Count" or "LongCount" or "First" or "FirstOrDefault" or "Single" or "SingleOrDefault" => "terminal",
        _ => "other",
    };
}
