// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using System.Diagnostics;
using System.Runtime.InteropServices;

internal class Profiler(
    ILogger logger,
    IGlobalOptions globalOptions,
    CancellationToken cancellationToken) : IProfiler
{
    private CancellationToken _cancellationToken = cancellationToken;

    public void Profile()
    {
        if (!globalOptions.TryGetProfilePath(out var profilePath))
        {
            return;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            logger.Log(
                new LogEntry
                {
                    Message = "Profiling is only supported on Windows.",
                    Severity = DiagnosticSeverity.Warning
                });
            
            return;
        }

        Directory.CreateDirectory(profilePath);
        var traceFile = Path.Combine(profilePath, $"pure_di_{Guid.NewGuid().ToString()[..4]}.dtt");
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var profiler = Path.Combine(userProfile, ".dotnet", "tools", "dottrace.exe");
        if (!File.Exists(profiler))
        {
            logger.Log(
                new LogEntry
                {
                    Message = $"Unable to find the \"{profiler}\" profiler. Please install the dotnet tool \"https://www.nuget.org/packages/JetBrains.dotTrace.GlobalTools\" globally.",
                    Severity = DiagnosticSeverity.Warning
                });
            
            return;
        }

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