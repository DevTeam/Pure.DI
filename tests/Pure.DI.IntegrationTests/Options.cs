namespace Pure.DI.IntegrationTests;

internal readonly record struct Options(
    LanguageVersion LanguageVersion = LanguageVersion.Latest,
    NullableContextOptions NullableContextOptions = NullableContextOptions.Enable,
    bool CheckCompilationErrors = true);