namespace Pure.DI;

internal static class DebugHelper
{
    [System.Diagnostics.Conditional("DEBUG_MODE")]
    public static void Debug()
    {
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            System.Diagnostics.Debugger.Launch();
        }
    }
}