namespace Pure.DI
{
    using System.Collections.Generic;
    using Core;
    using IoC;
    using Microsoft.CodeAnalysis;
    using static IoC.Lifetime;
    using static Core.Tags;
    using IBuildContext = Core.IBuildContext;

    internal class Configuration: IoC.IConfiguration
    {
        public IEnumerable<IToken> Apply(IMutableContainer container)
        {
            var autowiringStrategy = AutowiringStrategies
                .AspectOriented()
                .Tag<TagAttribute>(i => i.Tag);

            yield return container
                .Bind<IAutowiringStrategy>().To(ctx => autowiringStrategy)
                .Bind<IGenerator>().To<Generator>()
                .Bind<IStdOut>().As(ContainerSingleton).To<StdOut>()
                .Bind<IStdErr>().As(ContainerSingleton).To<StdErr>()
                .Bind<ITracer>().As(ContainerSingleton).To<Tracer>()
                .Bind<IDiagnostic>().As(ContainerSingleton).To<DefaultDiagnostic>()
                .Bind<ISourceBuilder>().To<SourceBuilder>()
                .Bind<IBuildContext>().As(ContainerSingleton).To<BuildContext>()
                .Bind<INameService>().Tag(Default).To<NameService>()
                .Bind<ITypeResolver>().Tag(Default).To<TypeResolver>()
                .Bind<ResolverMetadata>().To(ctx => ctx.Container.Inject<IBuildContext>().Metadata)
                .Bind<INameService>().To(ctx => ctx.Container.Inject<IBuildContext>().NameService)
                .Bind<ITypeResolver>().To(ctx => ctx.Container.Inject<IBuildContext>().TypeResolver)
                .Bind<IConstructorsResolver>().To<ConstructorsResolver>()
                .Bind<IObjectBuilder>().Tag(AutowiringBuilder).To<AutowiringObjectBuilder>()
                .Bind<IObjectBuilder>().Tag(FactoryBuilder).To<FactoryObjectBuilder>()
                .Bind<IObjectBuilder>().Tag(ArrayBuilder).To<ArrayObjectBuilder>()
                .Bind<IObjectBuilder>().Tag(EnumerableBuilder).To<EnumerableObjectBuilder>()
                .Bind<IFallbackStrategy>().As(ContainerSingleton).To<FallbackStrategy>()
                .Bind<IClassBuilder>().To<ClassBuilder>()
                .Bind<IMetadataWalker>().To(ctx => new MetadataWalker((SemanticModel) ctx.Args[0], ctx.Container.Inject<IOwnerProvider>(), ctx.Container.Inject<ITargetClassNameProvider>()))
                .Bind<IMembersBuilder>().Tag(Resolvers).To<ResolversBuilder>()
                .Bind<IMembersBuilder>().Tag(MicrosoftDependencyInjection).To<MicrosoftDependencyInjectionBuilder>()
                .Bind<IBindingsProbe>().As(ContainerSingleton).To<BindingsProbe>()
                .Bind<IBindingResultStrategy>().As(ContainerSingleton).Tag(AsIsResult).To<AsIsBindingResultStrategy>()
                .Bind<IBindingResultStrategy>().As(ContainerSingleton).Tag(GenericResult).To<GenericBindingResultStrategy>()
                .Bind<IResolveMethodBuilder>().Tag(StaticResolve).To<StaticResolveMethodBuilder>()
                .Bind<IResolveMethodBuilder>().Tag(StaticWithTag).To<StaticWithTagResolveMethodBuilder>()
                .Bind<IResolveMethodBuilder>().Tag(GenericStaticResolve).To<GenericStaticResolveMethodBuilder>()
                .Bind<IResolveMethodBuilder>().Tag(GenericStaticWithTag).To<GenericStaticWithTagResolveMethodBuilder>()
                .Bind<IBuildStrategy>().Tag(SimpleBuildStrategy).To<BuildStrategy>(
                    ctx => new BuildStrategy(
                        ctx.Container.Inject<IDiagnostic>(),
                        ctx.Container.Inject<ITracer>(),
                        ctx.Container.Inject<ILog<BuildStrategy>>(),
                        ctx.Container.Inject<IEnumerable<ILifetimeStrategy>>(),
                        ctx.Container.Inject<IBindingResultStrategy>(AsIsResult),
                        null))
                .Bind<IBuildStrategy>().Tag(GenericBuildStrategy).To<BuildStrategy>(
                    ctx => new BuildStrategy(
                        ctx.Container.Inject<IDiagnostic>(),
                        ctx.Container.Inject<ITracer>(),
                        ctx.Container.Inject<ILog<BuildStrategy>>(),
                        ctx.Container.Inject<IEnumerable<ILifetimeStrategy>>(),
                        ctx.Container.Inject<IBindingResultStrategy>(GenericResult),
                        ctx.Container.Inject<IBuildStrategy>(SimpleBuildStrategy)));
            
            yield return container
                .Bind<IBindingStatementsStrategy>().Tag(TypeStatementsStrategy).To<TypeBindingStatementsStrategy>()
                .Bind<IBindingStatementsStrategy>().Tag(TypeAndTagStatementsStrategy).To<TypeAndTagBindingStatementsStrategy>()
                .Bind<ITypesMap>().To<TypesMap>()
                .Bind<IAttributesService>().To<AttributesService>()
                .Bind<ILifetimeStrategy>().As(ContainerSingleton).Tag(Lifetime.Transient).To(ctx => new TransientLifetimeStrategy(Lifetime.Transient))
                .Bind<ILifetimeStrategy>().As(ContainerSingleton).Tag(Lifetime.ContainerSingleton).To(ctx => new TransientLifetimeStrategy(Lifetime.ContainerSingleton))
                .Bind<ILifetimeStrategy>().As(ContainerSingleton).Tag(Lifetime.Scoped).To(ctx => new TransientLifetimeStrategy(Lifetime.Scoped))
                .Bind<ILifetimeStrategy>().Tag(Lifetime.Singleton).To<SingletonLifetimeStrategy>()
                .Bind<ILifetimeStrategy>().Tag(Lifetime.PerThread).To<PerThreadLifetimeStrategy>()
                .Bind<ILifetimeStrategy>().Tag(Lifetime.PerResolve).To<PerResolveLifetimeStrategy>()
                .Bind<ILifetimeStrategy>().Tag(Lifetime.Binding).To<BindingLifetimeStrategy>()
                .Bind<ISyntaxRegistry>().To<SyntaxRegistry>()
                .Bind<ISettings>().To<Settings>()
                .Bind<ILog<IoC.TT>>().To<Log<IoC.TT>>()
                .Bind<IFileSystem>().As(ContainerSingleton).To<FileSystem>()
                .Bind<ITargetClassNameProvider>().To<TargetClassNameProvider>()
                .Bind<IOwnerProvider>().As(ContainerSingleton).To<OwnerProvider>();
        }
    }
}
