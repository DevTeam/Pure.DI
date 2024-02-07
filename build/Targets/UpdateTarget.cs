// ReSharper disable StringLiteralTypo
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable ClassNeverInstantiated.Global
namespace Build.Targets;

using NuGet.Versioning;

internal class UpdateTarget(
    Settings settings,
    ICommands commands,
    IPaths paths)
    : IInitializable, ITarget<NuGetVersion>
{
    private const string VersionPrefix = "PUREDI_API_V";

    public Task InitializeAsync() => commands.Register(
        this,
        "Updates internal DI version",
        "update",
        "u");
    
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public Task<NuGetVersion> RunAsync(CancellationToken cancellationToken)
    {
        Info("Updating internal DI version");
        var solutionDirectory = paths.SolutionDirectory;
        var currentVersion = settings.Version;
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
        WriteLine($"The internal version of Pure.DI has been updated to {currentVersion}.", Color.Details);
        
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