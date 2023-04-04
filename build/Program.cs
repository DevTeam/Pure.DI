// Run this from the working directory where the solution or project to build is located.

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

var readmeCommand = new Command("readme", "Generates README.MD");
readmeCommand.SetHandler(ctx => Composition.Resolve<ITarget<int>>(nameof(ReadmeTarget), settings).RunAsync(ctx));
readmeCommand.AddAlias("r");

var packCommand = new Command("pack", "Creates NuGet packages");
packCommand.SetHandler(ctx => Composition.Resolve<ITarget<string>>(nameof(PackTarget), settings).RunAsync(ctx));
packCommand.AddAlias("p");

var benchmarksCommand = new Command("benchmarks", "Runs benchmarks");
benchmarksCommand.SetHandler(ctx => Composition.Resolve<ITarget<int>>(nameof(BenchmarksTarget), settings).RunAsync(ctx));
benchmarksCommand.AddAlias("b");

var deployCommand = new Command("deploy", "Push NuGet packages");
deployCommand.SetHandler(ctx => Composition.Resolve<ITarget<int>>(nameof(DeployTarget), settings).RunAsync(ctx));
deployCommand.AddAlias("d");

var deployTemplateCommand = new Command("template", "Push NuGet packages");
deployTemplateCommand.SetHandler(ctx => Composition.Resolve<ITarget<string>>(nameof(DeployTemplateTarget), settings).RunAsync(ctx));
deployTemplateCommand.AddAlias("t");

return await new RootCommand
{
    readmeCommand,
    packCommand,
    benchmarksCommand,
    deployCommand,
    deployTemplateCommand
}.InvokeAsync(args);

#pragma warning disable CS0162
DI.Setup("Composition")
    .Arg<Settings>()
    .Bind<ITeamCityWriter>().To(_ => GetService<ITeamCityWriter>())
    .Bind<ITarget<int>>(nameof(ReadmeTarget)).To<ReadmeTarget>()
    .Bind<ITarget<string>>(nameof(PackTarget)).To<PackTarget>()
    .Bind<ITarget<int>>(nameof(BenchmarksTarget)).To<BenchmarksTarget>()
    .Bind<ITarget<int>>(nameof(DeployTarget)).To<DeployTarget>()
    .Bind<ITarget<string>>(nameof(DeployTemplateTarget)).To<DeployTemplateTarget>();