// ReSharper disable UnusedMember.Local
namespace Build;

using Pure.DI;

internal partial class Composition
{
    private static void Setup() =>
        DI.Setup("Composition")
            .Arg<Settings>("settings")
            .Bind<JetBrains.TeamCity.ServiceMessages.Write.Special.ITeamCityWriter>().As(Lifetime.PerResolve).To(_ => GetService<JetBrains.TeamCity.ServiceMessages.Write.Special.ITeamCityWriter>())
            .Bind<ITarget<int>>("readme").To<ReadmeTarget>()
            .Bind<ITarget<string>>("pack").To<PackTarget>()
            .Bind<ITarget<int>>("benchmarks").To<BenchmarksTarget>()
            .Bind<ITarget<int>>("deploy").To<DeployTarget>()
            .Bind<ITarget<string>>("template").To<TemplateTarget>()
            .Root<Program>("Root");
}