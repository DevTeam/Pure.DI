namespace Pure.DI.Core;

internal static class LogId
{
    // Error
    public const string ErrorUnableToResolve = "DIE000";
    public const string ErrorInvalidMetadata = "DIE001";
    public const string ErrorCannotFindSetup = "DIE002";
    public const string ErrorCyclicDependency = "DIE003";
    public const string ErrorNotSupportedLanguageVersion = "DIE004";
    public const string ErrorUnhandled = "DIE999";

    // Warning
    public const string WarningOverriddenBinding = "DIW000";
    public const string WarningMetadataDefect = "DIW001";
    public const string WarningRootArgInResolveMethod = "DIW002";

    // Info
    public const string InfoGenerationInterrupted = "DII000";
    public const string InfoMetadataDefect = "DII001";
}