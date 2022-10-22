// ReSharper disable RedundantTypeArgumentsOfMethod
namespace Pure.DI;

using Core;
using IoC;
using NS35EBD81B;
using static IoC.Lifetime;
using static Core.Tags;
using IBuildContext = Core.IBuildContext;

internal sealed class Configuration : IoC.IConfiguration
{
    public IEnumerable<IToken> Apply(IMutableContainer container)
    {
        var autowiringStrategy = AutowiringStrategies
            .AspectOriented()
            .Tag<TagAttribute>(i => i.Tag);

        yield return container
            .Bind<IAutowiringStrategy>().To(ctx => autowiringStrategy)
            .Bind<IGenerator>().To<Generator>()
            .Bind<ISyntaxFilter>().As(Singleton).Tag(MetadataSyntax).To<MetadataSyntaxFilter>()
            .Bind<ICannotResolveExceptionFactory>().As(ContainerSingleton).To<CannotResolveExceptionFactory>()
            .Bind<IStdOut>().As(ContainerSingleton).To<StdOut>()
            .Bind<IStdErr>().As(ContainerSingleton).To<StdErr>()
            .Bind<ITracer>().As(ContainerSingleton).To<Tracer>()
            .Bind<IDiagnostic>().As(ContainerSingleton).To<DefaultDiagnostic>()
            .Bind<IMetadataBuilder>().To<MetadataBuilder>()
            .Bind<ISourceBuilder>().To<SourceBuilder>()
            .Bind<IBuildContext>().As(ContainerSingleton).To<BuildContext>()
            .Bind<INameService>().Tag(Default).To<NameService>()
            .Bind<IMemberNameService>().To<MemberNameService>()
            .Bind<ITypeResolver>().Tag(Default).To<TypeResolver>()
            .Bind<ResolverMetadata>().To(ctx => ctx.Container.Inject<IBuildContext>().Metadata)
            .Bind<INameService>().To(ctx => ctx.Container.Inject<IBuildContext>().NameService)
            .Bind<ITypeResolver>().To(ctx => ctx.Container.Inject<IBuildContext>().TypeResolver)
            .Bind<IObjectBuilder>().Tag(AutowiringBuilder).To<AutowiringObjectBuilder>()
            .Bind<IObjectBuilder>().Tag(FactoryBuilder).To<FactoryObjectBuilder>()
            .Bind<IObjectBuilder>().Tag(ArrayBuilder).To<ArrayObjectBuilder>()
            .Bind<IObjectBuilder>().Tag(EnumerableBuilder).To<EnumerableObjectBuilder>()
            .Bind<IClassBuilder>().To<ClassBuilder>()
            .Bind<MetadataWalker>().To<MetadataWalker>()
            .Bind<Func<SemanticModel, IMetadataWalker>>().To(ctx => model => ctx.Container.Inject<MetadataWalker>().Initialize(model))
            .Bind<IMembersBuilder>().Tag(Resolvers).To<ResolversMembersBuilder>()
            .Bind<IMembersBuilder>().Tag(MicrosoftDependencyInjection).To<MicrosoftDependencyInjectionMembersBuilder>()
            .Bind<IArgumentsSupport>().To<ResolveContextMembersBuilder>()
            .Bind<IMembersBuilder>().Bind<IStatementsBlockWrapper>().Tag(ResolveContext).To<ResolveContextMembersBuilder>()
            .Bind<IBindingsProbe>().As(ContainerSingleton).To<BindingsProbe>()
            .Bind<IBindingResultStrategy>().As(ContainerSingleton).Tag(AsIsResult).To<AsIsBindingResultStrategy>()
            .Bind<IBindingResultStrategy>().As(ContainerSingleton).Tag(GenericResult).To<GenericBindingResultStrategy>()
            .Bind<IResolveMethodBuilder>().Tag(StaticResolve).To<StaticResolveMethodBuilder>()
            .Bind<IResolveMethodBuilder>().Tag(StaticWithTag).To<StaticWithTagResolveMethodBuilder>()
            .Bind<IResolveMethodBuilder>().Tag(GenericStaticResolve).To<GenericStaticResolveMethodBuilder>()
            .Bind<IResolveMethodBuilder>().Tag(GenericStaticWithTag).To<GenericStaticWithTagResolveMethodBuilder>()
            .Bind<IBuildStrategy>().To<BuildStrategy>()
            .Bind<ICache<IoC.TT, IoC.TT1>>().To<Cache<IoC.TT, IoC.TT1>>()
            .Bind<ICache<IoC.TT, IoC.TT1>>().As(ContainerSingleton).Tag(ContainerScope).To<Cache<IoC.TT, IoC.TT1>>()
            .Bind<ICache<IoC.TT, IoC.TT1>>().As(Singleton).Tag(GlobalScope).To<Cache<IoC.TT, IoC.TT1>>();

        yield return container
            .Bind<ITypesMap>().To<TypesMap>()
            .Bind<IAttributesService>().To<AttributesService>()
            .Bind<ILifetimeStrategy>().Tag(NS35EBD81B.Lifetime.Transient).To<TransientLifetimeStrategy>()
            .Bind<MicrosoftDependencyInjectionLifetimeStrategy>().To<MicrosoftDependencyInjectionLifetimeStrategy>()
            .Bind<ILifetimeStrategy>().Tag(NS35EBD81B.Lifetime.ContainerSingleton).To<MicrosoftDependencyInjectionLifetimeStrategy>(ctx => ctx.Container.Inject<MicrosoftDependencyInjectionLifetimeStrategy>().Initialize(NS35EBD81B.Lifetime.ContainerSingleton))
            .Bind<ILifetimeStrategy>().Tag(NS35EBD81B.Lifetime.Scoped).To<MicrosoftDependencyInjectionLifetimeStrategy>(ctx => ctx.Container.Inject<MicrosoftDependencyInjectionLifetimeStrategy>().Initialize(NS35EBD81B.Lifetime.Scoped))
            .Bind<ILifetimeStrategy>().Tag(NS35EBD81B.Lifetime.Singleton).To<SingletonLifetimeStrategy>()
            .Bind<ILifetimeStrategy>().Tag(NS35EBD81B.Lifetime.PerResolve).To<PerResolveLifetimeStrategy>()
            .Bind<IWrapperStrategy>().To<CompositeWrapperStrategy>()
            .Bind<IWrapperStrategy>().Tag(Factory).To<FactoryWrapperStrategy>()
            .Bind<IWrapperStrategy>().Tag(FactoryMethod).To<FactoryMethodWrapperStrategy>()
            .Bind<ISyntaxRegistry>().To<SyntaxRegistry>()
            .Bind<ISettings>().To<Settings>()
            .Bind<ILog<IoC.TT>>().To<Log<IoC.TT>>()
            .Bind<IFileSystem>().As(ContainerSingleton).To<FileSystem>()
            .Bind<ITargetClassNameProvider>().To<TargetClassNameProvider>()
            .Bind<IOwnerProvider>().As(ContainerSingleton).To<OwnerProvider>()
            .Bind<IDisposeStatementsBuilder>().As(Singleton).To<DisposeStatementsBuilder>()
            .Bind<IRaiseOnDisposableExpressionBuilder>().As(Singleton).To<RaiseOnDisposableExpressionBuilder>()
            .Bind<IIncludeTypeFilter>().As(Singleton).To<IncludeTypeFilter>()
            .Bind<IMembersBuilder>().Tag(GenericResolvers).To<GenericResolversMembersBuilder>()
            .Bind<IMembersBuilder>().Tag(SharedContext).To<SharedContextMembersBuilder>()
            .Bind<IMembersBuilder>().Tag(EventField).To<EventFieldMembersBuilder>()
            .Bind<IMembersBuilder>().Tag(Dispose).To<DisposeMembersBuilder>()
            .Bind<IMembersBuilder>().Tag(ContextClass).To<ContextClassMembersBuilder>()
            .Bind<IMembersBuilder>().Tag(RunInContext).To<RunInContextMembersBuilder>()
            .Bind<IStringTools>().As(Singleton).To<StringTools>()
            .Bind<IStatementsBlockWrapper>().Bind<IMembersBuilder>().To<StatementsBlockWrapperMembersBuilder>()
            .Bind<IStaticResolverNameProvider>().To<StaticResolverNameProvider>()
            .Bind<IAccessibilityToSyntaxKindConverter>().As(Singleton).To<AccessibilityToSyntaxKindConverter>()
            .Bind<ICompilationUnitSyntaxBuilder>().To<CompilationUnitSyntaxBuilder>()
            .Bind<IMetadataFactory>().As(Singleton).To<MetadataFactory>()
            .Bind<IApiGuard>().As(Singleton).To<ApiGuard>()
            .Bind<IDependencyAccessibility>().As(Singleton).To<DependencyAccessibility>();
    }
}