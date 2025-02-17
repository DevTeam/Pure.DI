namespace Pure.DI.IntegrationTests;

record Options(
    LanguageVersion LanguageVersion = LanguageVersion.CSharp8,
    NullableContextOptions NullableContextOptions = NullableContextOptions.Enable,
    bool CheckCompilationErrors = true,
    ImmutableArray<string> PreprocessorSymbols = default);