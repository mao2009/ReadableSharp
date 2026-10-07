using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadableSharp.Tests;

internal static class AnalyzerTestHarness
{
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        IReadOnlyDictionary<string, string>? options = null)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));

        var compilation = CSharpCompilation.Create(
            "ReadableSharpAnalyzerTests",
            new[] { syntaxTree },
            GetPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzerOptions = new AnalyzerOptions(
            ImmutableArray<AdditionalText>.Empty,
            new DictionaryAnalyzerConfigOptionsProvider(options));

        return await compilation
            .WithAnalyzers(ImmutableArray.Create(analyzer), analyzerOptions)
            .GetAnalyzerDiagnosticsAsync();
    }

    private static IEnumerable<MetadataReference> GetPlatformReferences()
    {
        var trustedPlatformAssemblies =
            (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
    }

    private sealed class DictionaryAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _options;

        public DictionaryAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string>? values)
        {
            _options = new DictionaryAnalyzerConfigOptions(values);
        }

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    private sealed class DictionaryAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public DictionaryAnalyzerConfigOptions(IReadOnlyDictionary<string, string>? values)
        {
            _values = values ?? new Dictionary<string, string>();
        }

        public override bool TryGetValue(string key, out string value)
        {
            if (_values.TryGetValue(key, out var configured))
            {
                value = configured;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
