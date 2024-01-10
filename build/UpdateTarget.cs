// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
namespace Build;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using HostApi;
using NuGet.Versioning;

internal class UpdateTarget: Command, ITarget<NuGetVersion>
{
    private const string VersionPrefix = "PUREDI_API_V";
    private readonly Settings _settings;

    public UpdateTarget(
        Settings settings)
        : base("update", "Updates internal DI version")
    {
        _settings = settings;
        this.SetHandler(RunAsync);
        AddAlias("u");
    }
    
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public Task<NuGetVersion> RunAsync(InvocationContext ctx)
    {
        var solutionDirectory = Tools.GetSolutionDirectory();
        var currentVersion = _settings.VersionOverride ?? new NuGetRestoreSettings("Pure.DI").GetNextVersion(_settings.VersionRange, 0);
        var propsFile = Path.Combine(solutionDirectory, "Directory.Build.props");
        var props = File.ReadAllLines(propsFile);
        var contents = new List<string>();
        foreach (var prop in props)
        {
            var line = prop;
            var index = line.IndexOf("<InternalVersion>", StringComparison.InvariantCulture);
            if (index >= 0)
            {
                line = $"{new string(' ', index)}<InternalVersion>{currentVersion}</InternalVersion>";
            }
            
            contents.Add(line);
        }
        
        File.WriteAllLines(propsFile, contents);
        WriteLine($"The internal version of Pure.DI has been updated to {currentVersion}.", Color.Highlighted);
        
        var projectDir = Path.Combine(solutionDirectory, "src", "Pure.DI.Core");
        var files =
            Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Concat(Enumerable.Repeat(Path.Combine(projectDir, "Pure.DI.Core.csproj"), 1));

        foreach (var file in files)
        {
            contents.Clear();
            var hasVersion = false;
            foreach (var line in File.ReadLines(file))
            {
                var newLine = line;
                var index = newLine.IndexOf(VersionPrefix, StringComparison.InvariantCulture);
                if (index >= 0 && index + VersionPrefix.Length < line.Length)
                {
                    var version = newLine[index + VersionPrefix.Length];
                    switch (version)
                    {
                        case '1':
                            newLine = newLine.Replace(VersionPrefix + "1", VersionPrefix + "2");
                            hasVersion = true;
                            break;
                        
                        case '2':
                            newLine = newLine.Replace(VersionPrefix + "2", VersionPrefix + "1");
                            hasVersion = true;
                            break;
                    }
                }
                
                contents.Add(newLine);
            }

            if (hasVersion)
            {
                File.WriteAllLines(file, contents);
            }
        }

        new DotNetBuildServerShutdown().Run();
        new DotNetRestore().Run();
        new DotNetBuild().Run();
        return Task.FromResult(currentVersion);
    }
}