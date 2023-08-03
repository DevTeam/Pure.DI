namespace Pure.DI;

using System.Diagnostics;

internal static class DebugHelper
{
    [Conditional("DEBUG_MODE")]
    public static void Debug()
    {
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
    }

    [Conditional("TRACE_MODE")]
    public static void Trace()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var outputDirectory = Path.Combine(userProfile, ".pure-di");
        Directory.CreateDirectory(outputDirectory);
        var traceFile = Path.Combine(outputDirectory, $"dotTrace{Guid.NewGuid().ToString()[..4]}.dtt");
        var profiler = Path.Combine(userProfile, ".dotnet", "tools", "dottrace.exe");
        var traceProcess = new Process
        {
            StartInfo = new ProcessStartInfo(
                profiler,
                $"""attach {Process.GetCurrentProcess().Id} --save-to="{traceFile}" --profiling-type=Sampling --timeout=30s""")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            }
        };

        traceProcess.Start();
        Thread.Sleep(2000);
    }
}