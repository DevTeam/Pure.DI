﻿// Run this from the working directory where the solution or project to build is located.

using System.CommandLine;
using Build;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using NuGet.Versioning;
using Pure.DI;

Directory.SetCurrentDirectory(Tools.GetSolutionDirectory());
var version = NuGetVersion.Parse(Property.Get("version", "2.0.0-dev", true));
var nuGetKey = Property.Get("NuGetKey", string.Empty);
var requiredSdkRange = VersionRange.Parse(Property.Get("RequiredSdkRange", "[7.0, )"), false);
var configuration = Environment.OSVersion.Platform == PlatformID.Win32NT ? "Release" : "Linux";
var settings = new Settings(
    configuration,
    version,
    nuGetKey,
    requiredSdkRange,
    new BuildCase(new Version(4, 3, 1)));

var composition = new Composition(settings);
return await composition.Root.RunAsync();

#pragma warning disable CS0162
// ReSharper disable once HeuristicUnreachableCode
DI.Setup("Composition")
    .Arg<Settings>("settings")
    .Bind<ITeamCityWriter>().To(_ => GetService<ITeamCityWriter>())
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
