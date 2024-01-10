// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
namespace Build;

using System.CommandLine;
using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using Pure.DI;

internal partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Root<RootCommand>("Root")
            .Arg<Settings>("settings")
            .Hint(Hint.Resolve, "Off")
            .Bind<ITeamCityWriter>().To(_ => GetService<ITeamCityWriter>())
            .Bind<INuGet>().To(_ => GetService<INuGet>())
            .Bind<Command>(typeof(ReadmeTarget)).To<ReadmeTarget>()
            .Bind<Command, ITarget<IReadOnlyCollection<string>>>(typeof(PackTarget)).To<PackTarget>()
            .Bind<Command, ITarget<int>>(typeof(BenchmarksTarget)).To<BenchmarksTarget>()
            .Bind<Command>(typeof(DeployTarget)).To<DeployTarget>()
            .Bind<Command>(typeof(TemplateTarget)).To<TemplateTarget>()
            .Bind<Command>(typeof(UpdateTarget)).To<UpdateTarget>()
            .Bind<RootCommand>().To(ctx =>
            {
                var rootCommand = new RootCommand();
                ctx.Inject(out IEnumerable<Command> commands);
                foreach (var command in commands)
                {
                    rootCommand.AddCommand(command);
                }

                return rootCommand;
            });
}