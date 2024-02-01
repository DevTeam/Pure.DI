using Build;
using IProperties = Build.Tools.IProperties;

DI.Setup(nameof(Composition))
    .Root<ITarget<int>>("RootTarget")
    .DefaultLifetime(Lifetime.PerBlock)
    .Bind<Settings>().To<Settings>()
    .Bind<RootCommand>().To<RootCommand>()
    .Bind<ITeamCityArtifactsWriter>().To(_ => GetService<ITeamCityWriter>())
    .Bind<INuGet>().To(_ => GetService<INuGet>())
    // Tools
    .Bind<ICommands>().To<Commands>()
    .Bind<IPaths>().To<Paths>()
    .Bind<IProperties>().To<Properties>()
    .Bind<ISdk>().To<Sdk>()
    .Bind<IVersions>().To<Versions>()
    // Targets
    .Bind<ITarget<int>>().To<RootTarget>()
    .Bind<IInitializable, ITarget<BuildResult>>(typeof(TestTarget)).To<TestTarget>()
    .Bind<IInitializable, ITarget<BuildResult>>(typeof(CompatibilityCheckTarget)).To<CompatibilityCheckTarget>()
    .Bind<IInitializable, ITarget<BuildResult>>(typeof(PackTarget)).To<PackTarget>()
    .Bind<IInitializable>(typeof(ReadmeTarget)).To<ReadmeTarget>()
    .Bind<IInitializable, ITarget<int>>(typeof(BenchmarksTarget)).To<BenchmarksTarget>()
    .Bind<IInitializable>(typeof(DeployTarget)).To<DeployTarget>()
    .Bind<IInitializable>(typeof(TemplateTarget)).To<TemplateTarget>()
    .Bind<IInitializable>(typeof(UpdateTarget)).To<UpdateTarget>();

return await new Composition().RootTarget.RunAsync(CancellationToken.None);