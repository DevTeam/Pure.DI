using Build;
using HostApi;
using System.CommandLine;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Root<Program>("Root")
    .Bind<ITeamCityWriter>().To(_ => GetService<ITeamCityWriter>())
    .Bind<INuGet>().To(_ => GetService<INuGet>())
    .DefaultLifetime(Lifetime.Singleton)
    .Bind<Settings>().To<Settings>()
    .Bind<Command>(typeof(ReadmeTarget)).To<ReadmeTarget>()
    .Bind<Command, ITarget<IReadOnlyCollection<string>>>(typeof(PackTarget)).To<PackTarget>()
    .Bind<Command, ITarget<int>>(typeof(BenchmarksTarget)).To<BenchmarksTarget>()
    .Bind<Command>(typeof(DeployTarget)).To<DeployTarget>()
    .Bind<Command>(typeof(TemplateTarget)).To<TemplateTarget>()
    .Bind<Command>(typeof(UpdateTarget)).To<UpdateTarget>();

await new Composition().Root.RunAsync();

internal partial class Program(
    RootCommand rootCommand,
    Paths paths,
    IEnumerable<Command> commands)
{
    private async Task RunAsync()
    {
        foreach (var command in commands)
        {
            rootCommand.AddCommand(command);
        }

        Directory.SetCurrentDirectory(paths.GetSolutionDirectory());
        await new DotNetBuildServerShutdown().RunAsync();
        await rootCommand.InvokeAsync(Args.ToArray());
    }
}