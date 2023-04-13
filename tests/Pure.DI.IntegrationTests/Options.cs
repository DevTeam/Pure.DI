namespace Pure.DI.IntegrationTests;

internal record Options(
    LanguageVersion LanguageVersion = LanguageVersion.CSharp8,
    NullableContextOptions NullableContextOptions = NullableContextOptions.Enable,
    bool CheckCompilationErrors = true);