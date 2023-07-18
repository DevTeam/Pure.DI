namespace Pure.DI.Core;

internal static class LogId
{
    // Errors
    public const string ErrorUnresolvedDependency = "DIE000";
    public const string ErrorCyclicDependency = "DIE001";
    public const string ErrorCannotFindSetup = "DIE002";
    public const string ErrorInvalidMetadata = "DIE003";
    public const string ErrorNotSupportedLanguageVersion = "DIE004";
    public const string ErrorUnhandled = "DIE999";

    // Warning
    public const string WarningOverriddenBinding = "DIW000";
    public const string WarningMetadataDefect = "DIW001";

    // Info
    public const string InfoGenerationInterrupted = "DII000";
}