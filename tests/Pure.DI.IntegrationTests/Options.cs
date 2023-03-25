namespace Pure.DI.IntegrationTests;

internal record Options(
    LanguageVersion LanguageVersion = LanguageVersion.CSharp9,
    NullableContextOptions NullableContextOptions = NullableContextOptions.Enable,
    bool CheckCompilationErrors = true);