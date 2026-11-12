using CSharpLanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;

namespace Pure.DI.IntegrationTests;

record Options(
    LanguageVersion LanguageVersion = LanguageVersion.CSharp8,
    NullableContextOptions NullableContextOptions = NullableContextOptions.Enable,
    bool CheckCompilationErrors = true,
    ImmutableArray<string> PreprocessorSymbols = default,
    string? Culture = "",
    ImmutableArray<MetadataReference> MetadataReferences = default)
{
    // LanguageVersion.CSharp12 was added in Roslyn 4.8, so the tests compiled against Roslyn 4.3
    // run with the newest language version that its parser knows.
#if ROSLYN4_8_OR_GREATER
    public const LanguageVersion ModernLanguageVersion = CSharpLanguageVersion.CSharp12;
#else
    public const LanguageVersion ModernLanguageVersion = CSharpLanguageVersion.CSharp10;
#endif

    // Cancels the generation, for example, when it takes too long.
    public CancellationToken CancellationToken { get; init; }
}
