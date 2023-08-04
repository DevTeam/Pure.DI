using System.CommandLine;
using Build;using HostApi;
using NuGet.Versioning;

var defaultVersionRange = VersionRange.Parse("2.0.*");

NuGetVersion? versionOverride = default;
if (NuGetVersion.TryParse(Property.Get("version", ""), out var versionOverrideValue))
{
    WriteLine($"The version has been overridden by {versionOverrideValue}.", Color.Highlighted);
    versionOverride = versionOverrideValue;
}
else
{
    WriteLine("The next version has been used.", Color.Highlighted);
}

Directory.SetCurrentDirectory(Tools.GetSolutionDirectory());
var settings = new Settings(
    Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null,
    "Release",
    defaultVersionRange,
    versionOverride,
    Property.Get("NuGetKey", string.Empty),
    new CodeAnalysis(new Version(4, 3, 1)));

new DotNetBuildServerShutdown().Run();

var composition = new Composition(settings);
return await composition.Root.RunAsync();

internal partial class Program
{
    private readonly RootCommand _rootCommand;

    public Program(IEnumerable<ICommandProvider> commandProviders)
    {
        _rootCommand = new RootCommand();
        foreach (var command in commandProviders.Select(i => i.Command))
        {
            WriteLine($"{command.Name} added: {command.Description}", Color.Highlighted);
            _rootCommand.AddCommand(command);
        }
    }

    private Task<int> RunAsync() => _rootCommand.InvokeAsync(Args.ToArray());
}
