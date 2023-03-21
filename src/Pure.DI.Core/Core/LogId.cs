namespace Pure.DI.Core;

internal static class LogId
{
    // Errors
    public const string ErrorUnhandled = "DIE0000";
    public const string ErrorCircularDependency = "DIE0001";
    public const string ErrorCannotFindSetup = "DIE0002";
    public const string ErrorInvalidMetadata = "DIE0003";
    public const string ErrorUnresolved = "DIE0004";
    
    // Warning
    public const string WarningOverriddenBinding = "DIW0000";
    
    // Info
    public const string InfoGenerationInterrupted = "DII0000";
}