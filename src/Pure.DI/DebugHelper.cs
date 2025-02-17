namespace Pure.DI;

using System.Diagnostics;

static class DebugHelper
{
    [Conditional("DEBUG_MODE")]
    public static void Debug()
    {
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
    }
}