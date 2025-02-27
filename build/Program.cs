using Build;
using Build.Core;
using Build.Core.Doc;
using Build.Core.Targets;
using static Pure.DI.Lifetime;

DI.Setup(nameof(Composition))
    .Hint(Hint.ThreadSafe, "Off")
    .Hint(Hint.Resolve, "Off")
    .Root<RootTarget>(nameof(Composition.Root))
    .DefaultLifetime(PerResolve)
    .Bind().To<RootCommand>()
    .Bind().To<Settings>()
    .DefaultLifetime(PerBlock)
    .Bind<ITeamCityArtifactsWriter>().To(_ => GetService<ITeamCityWriter>())
    .Bind().To(_ => GetService<INuGet>())
    .Bind().To<DotNetEnv>()
    .Bind().To<Markdown>()
    .Bind().To<XDocumentTools>()
    .Bind().To<DotNetXmlDocumentWalker<TT>>()
    .Bind().To<MarkdownWriterVisitor>()
    .Bind().To<DocumentParts>()

    // Targets
    .Bind(Tag.Type).To<GeneratorTarget>()
    .Bind(Tag.Type).To<LibrariesTarget>()
    .Bind(Tag.Type).To<CompatibilityCheckTarget>()
    .Bind(Tag.Type).To<PackTarget>()
    .Bind(Tag.Type).To<CreateExamplesTarget>()
    .Bind(Tag.Type).To<ReadmeTarget>()
    .Bind(Tag.Type).To<TestExamplesTarget>()
    .Bind(Tag.Type).To<BenchmarksTarget>()
    .Bind(Tag.Type).To<DeployTarget>()
    .Bind(Tag.Type).To<TemplateTarget>()
    .Bind(Tag.Type).To<InstallTemplateTarget>()
    .Bind(Tag.Type).To<UpdateTarget>()
    .Bind(Tag.Type).To<PublishBlazorTarget>()
    .Bind(Tag.Type).To<PerformanceTestsTarget>();

return await new Composition().Root.RunAsync(CancellationToken.None);