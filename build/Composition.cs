// ReSharper disable UnusedMember.Local
namespace Build;

using Pure.DI;

internal partial class Composition
{
    private static void Setup() =>
        DI.Setup("Composition")
            .Arg<Settings>("settings")
            .Bind<JetBrains.TeamCity.ServiceMessages.Write.Special.ITeamCityWriter>().As(Lifetime.PerResolve).To(_ => GetService<JetBrains.TeamCity.ServiceMessages.Write.Special.ITeamCityWriter>())
            .Bind<ITarget<int>>().Bind<ICommandProvider>().Tags(nameof(ReadmeTarget)).To<ReadmeTarget>()
            .Bind<ITarget<string>>().Bind<ICommandProvider>().Tags(nameof(PackTarget)).To<PackTarget>()
            .Bind<ITarget<int>>().Bind<ICommandProvider>().Tags(nameof(BenchmarksTarget)).To<BenchmarksTarget>()
            .Bind<ITarget<int>>().Bind<ICommandProvider>().Tags(nameof(DeployTarget)).To<DeployTarget>()
            .Bind<ITarget<string>>().Bind<ICommandProvider>().Tags(nameof(TemplateTarget)).To<TemplateTarget>()
            .Root<Program>("Root");
}