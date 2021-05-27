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
                .Bind<IMemberNameService>().To<MemberNameService>()
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
                    ctx => ctx.Container.Assign(ctx.It.ResultStrategy, ctx.Container.Inject<IBindingResultStrategy>(AsIsResult)))
                .Bind<IBuildStrategy>().Tag(GenericBuildStrategy).To<BuildStrategy>(
                    ctx => ctx.Container.Assign(ctx.It.ResultStrategy, ctx.Container.Inject<IBindingResultStrategy>(GenericResult)),
                    ctx => ctx.Container.Assign(ctx.It.DependencyBindingExpressionStrategy, ctx.Container.Inject<IBuildStrategy>(SimpleBuildStrategy)));
            
            yield return container
                .Bind<IBindingStatementsStrategy>().Tag(TypeStatementsStrategy).To<TypeBindingStatementsStrategy>()
                .Bind<IBindingStatementsStrategy>().Tag(TypeAndTagStatementsStrategy).To<TypeAndTagBindingStatementsStrategy>()
                .Bind<ITypesMap>().To<TypesMap>()
                .Bind<IAttributesService>().To<AttributesService>()
                .Bind<ILifetimeStrategy>().As(Singleton).Tag(Lifetime.Transient).To<TransientLifetimeStrategy>()
                .Bind<ILifetimeStrategy>().Tag(Lifetime.ContainerSingleton).To<MicrosoftDependencyInjectionLifetimeStrategy>(ctx => ctx.Container.Assign(ctx.It.Lifetime, Lifetime.ContainerSingleton))
                .Bind<ILifetimeStrategy>().Tag(Lifetime.Scoped).To<MicrosoftDependencyInjectionLifetimeStrategy>(ctx => ctx.Container.Assign(ctx.It.Lifetime, Lifetime.Scoped))
                .Bind<ILifetimeStrategy>().Tag(Lifetime.Singleton).To<SingletonLifetimeStrategy>()
                .Bind<ILifetimeStrategy>().Tag(Lifetime.PerThread).To<PerThreadLifetimeStrategy>()
                .Bind<ILifetimeStrategy>().Tag(Lifetime.PerResolve).To<PerResolveLifetimeStrategy>()
                .Bind<ILifetimeStrategy>().Tag(Lifetime.Binding).To<BindingLifetimeStrategy>()
                .Bind<ISyntaxRegistry>().To<SyntaxRegistry>()
                .Bind<ISettings>().To<Settings>()
                .Bind<ILog<IoC.TT>>().To<Log<IoC.TT>>()
                .Bind<IFileSystem>().As(ContainerSingleton).To<FileSystem>()
                .Bind<ITargetClassNameProvider>().To<TargetClassNameProvider>()
                .Bind<IOwnerProvider>().As(ContainerSingleton).To<OwnerProvider>()
                .Bind<IDisposeStatementsBuilder>().As(Singleton).To<DisposeStatementsBuilder>();
        }
    }
}
