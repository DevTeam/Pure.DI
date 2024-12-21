using Build;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Root<RootTarget>("RootTarget")
    .DefaultLifetime(PerBlock)
    .Bind().To<RootCommand>()
    .Bind().To<Settings>()
    .Bind<ITeamCityArtifactsWriter>().To(_ => GetService<ITeamCityWriter>())
    .Bind().To(_ => GetService<INuGet>())
    .Bind(Tag.Type).To<DotNetEnv>()

    // Targets
    .Bind(Tag.Type).To<GeneratorTarget>()
    .Bind(Tag.Type).To<LibrariesTarget>()
    .Bind(Tag.Type).To<CompatibilityCheckTarget>()
    .Bind(Tag.Type).To<PackTarget>()
    .Bind(Tag.Type).To<ReadmeTarget>()
    .Bind(Tag.Type).To<BenchmarksTarget>()
    .Bind(Tag.Type).To<DeployTarget>()
    .Bind(Tag.Type).To<TemplateTarget>()
    .Bind(Tag.Type).To<InstallTemplateTarget>()
    .Bind(Tag.Type).To<UpdateTarget>()
    .Bind(Tag.Type).To<PublishBlazorTarget>()
    .Bind(Tag.Type).To<PerformanceTestsTarget>();

return await new Composition().RootTarget.RunAsync(CancellationToken.None);