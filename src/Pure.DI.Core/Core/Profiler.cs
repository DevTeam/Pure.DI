// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using System.Diagnostics;

internal class Profiler(CancellationToken cancellationToken) : IProfiler
{
    private CancellationToken _cancellationToken = cancellationToken;

    public void Profiling(string path)
    {
        Directory.CreateDirectory(path);
        var traceFile = Path.Combine(path, $"pure_di_{Guid.NewGuid().ToString()[..4]}.dtt");
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
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

        _cancellationToken.Register(state =>
        {
            var process = (Process)state;
            if (!process.HasExited)
            {
                process.Kill();
            }
        }, traceProcess);

        traceProcess.Start();
        Thread.Sleep(TimeSpan.FromSeconds(2));
    }
}