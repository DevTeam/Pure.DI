using System.CommandLine;
using Build;using HostApi;
using NuGet.Versioning;

var autoIncrementingVersionRange = VersionRange.Parse("2.0.*");

WriteLine(
    NuGetVersion.TryParse(Property.Get("version"), out var versionOverride) 
        ? $"The version has been overridden by {versionOverride}."
        : "The next version has been used.",
    Color.Highlighted);

Directory.SetCurrentDirectory(Tools.GetSolutionDirectory());
var settings = new Settings(
    Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null,
    "Release",
    autoIncrementingVersionRange,
    versionOverride,
    Property.Get("NuGetKey"),
    new CodeAnalysis(new Version(4, 3, 1)));

return await new Composition(settings).Root.RunAsync(args);

internal partial class Program
{
    private readonly IEnumerable<Command> _commands;

    public Program(IEnumerable<Command> commands) => 
        _commands = commands;

    private async Task<int> RunAsync(string[] args)
    {
        var rootCommand = new RootCommand();
        foreach (var command in _commands)
        {
            rootCommand.AddCommand(command);
        }
        
        await new DotNetBuildServerShutdown().RunAsync();
        return await rootCommand.InvokeAsync(args);
    }
}
