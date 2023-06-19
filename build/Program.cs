// Run this from the working directory where the solution or project to build is located.

using System.CommandLine;
using Build;using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using NuGet.Versioning;
using Pure.DI;

Directory.SetCurrentDirectory(Tools.GetSolutionDirectory());
var settings = new Settings(
    "Release",
    VersionRange.Parse(Property.Get("version", "2.*-*", true)),
    Property.Get("NuGetKey", string.Empty),
    new BuildCase(new Version(4, 3, 1)));

new DotNetBuildServerShutdown().Run();

var composition = new Composition(settings);
return await composition.Root.RunAsync();

#pragma warning disable CS0162
// ReSharper disable once HeuristicUnreachableCode
DI.Setup("Composition")
    .Arg<Settings>("settings")
    .Bind<ITeamCityWriter>().As(Lifetime.PerResolve).To(_ => GetService<ITeamCityWriter>())
    .Bind<ITarget<int>>("readme").To<ReadmeTarget>()
    .Bind<ITarget<string>>("pack").To<PackTarget>()
    .Bind<ITarget<int>>("benchmarks").To<BenchmarksTarget>()
    .Bind<ITarget<int>>("deploy").To<DeployTarget>()
    .Bind<ITarget<string>>("template").To<TemplateTarget>()
    .Root<Program>("Root");
    
internal partial class Program
{
    private readonly RootCommand _rootCommand;

    public Program(
        [Tag("readme")] ITarget<int> readme,
        [Tag("pack")] ITarget<string> pack,
        [Tag("benchmarks")] ITarget<int> benchmarks,
        [Tag("deploy")] ITarget<int> deploy,
        [Tag("template")] ITarget<string> template)
    {
        var readmeCommand = new Command("readme", "Generates README.MD");
        readmeCommand.SetHandler(readme.RunAsync);
        readmeCommand.AddAlias("r");

        var packCommand = new Command("pack", "Creates NuGet packages");
        packCommand.SetHandler(pack.RunAsync);
        packCommand.AddAlias("p");

        var benchmarksCommand = new Command("benchmarks", "Runs benchmarks");
        benchmarksCommand.SetHandler(benchmarks.RunAsync);
        benchmarksCommand.AddAlias("b");

        var deployCommand = new Command("deploy", "Push NuGet packages");
        deployCommand.SetHandler(deploy.RunAsync);
        deployCommand.AddAlias("d");

        var deployTemplateCommand = new Command("template", "Push NuGet packages");
        deployTemplateCommand.SetHandler(template.RunAsync);
        deployTemplateCommand.AddAlias("t");

        _rootCommand = new RootCommand
        {
            readmeCommand,
            packCommand,
            benchmarksCommand,
            deployCommand,
            deployTemplateCommand
        };
    }

    private Task<int> RunAsync() => _rootCommand.InvokeAsync(Args.ToArray());
}
