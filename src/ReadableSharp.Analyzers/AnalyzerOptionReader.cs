using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadableSharp.Analyzers;

internal static class AnalyzerOptionReader
{
    public static int GetInt32(
        AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string key,
        int defaultValue,
        int minValue = 1,
        int maxValue = 1000)
    {
        var provider = options.AnalyzerConfigOptionsProvider;
        var treeOptions = provider.GetOptions(syntaxTree);

        if (TryRead(treeOptions, key, minValue, maxValue, out var value))
        {
            return value;
        }

        return TryRead(provider.GlobalOptions, key, minValue, maxValue, out value)
            ? value
            : defaultValue;
    }

    private static bool TryRead(
        AnalyzerConfigOptions options,
        string key,
        int minValue,
        int maxValue,
        out int value)
    {
        value = default;

        return options.TryGetValue(key, out var raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
            && value >= minValue
            && value <= maxValue;
    }
}
