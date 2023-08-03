// ReSharper disable UnusedMember.Local
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI;

// ReSharper disable once PartialTypeWithSinglePart
internal partial class Composition
{
    private static void Setup() => DI.Setup(nameof(Composition))
        .DependsOn(nameof(CompositionBase))
        .Arg<IOptions>("options")
        .Arg<ISourcesRegistry>("sources")
        .Arg<IDiagnostic>("diagnostic")
        .Arg<CancellationToken>("cancellationToken")

        // Transient
        .DefaultLifetime(Lifetime.Transient)
        .Bind<IMetadataSyntaxWalker>().To<MetadataSyntaxWalker>()
        .Bind<IDependencyGraphBuilder>().To<DependencyGraphBuilder>()
        .Bind<IVarIdGenerator>().To<VarIdGenerator>()
        .Bind<ITypeConstructor>().To<TypeConstructor>()
        .Bind<IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>>().To<SetupsBuilder>()
        .Bind<IBuilder<IEnumerable<SyntaxUpdate>, Unit>>().To<Core.Generator>()
        .Bind<IBuilder<RewriterContext<MdFactory>, MdFactory>>().To<FactoryTypeRewriter>()
        .Bind<IBuilder<DependencyGraph, CompositionCode>>(WellknownTag.CompositionBuilder).To<CompositionBuilder>()

        // PerResolve
        .DefaultLifetime(Lifetime.PerResolve)
        .Bind<IInformation>().To<Information>()
        .Bind<IClock>().To<Clock>()
        .Bind<IFileSystem>().To<FileSystem>()
        .Bind<ILogger<TT>>().To<Logger<TT>>()
        .Bind<ILogObserver>().To<LogObserver>()
        .Bind<IGlobalOptions>().To<GlobalOptions>()
        .Bind<IFormatting>().To<Formatting>()
        .Bind<IMarker>().To<Marker>()
        .Bind<IFilter>().To<Filter>()
        .Bind<IVariator<TT>>().To<Variator<TT>>()
        .Bind<IUnboundTypeConstructor>().To<UnboundTypeConstructor>()
        .Bind<IEqualityComparer<string>>().To(_ => StringComparer.InvariantCultureIgnoreCase)
        .Bind<Func<ImmutableArray<byte>, bool>>().To(_ => new Func<ImmutableArray<byte>, bool>(_ => true))
        .Bind<IValidator<DependencyGraph>>().To<DependencyGraphValidator>()
        .Bind<IValidator<MdSetup>>().To<MetadataValidator>()
        .Bind<IApiInvocationProcessor>().To<ApiInvocationProcessor>()
        .Bind<IBuilder<LogEntry, LogInfo>>().To<LogInfoBuilder>()
        .Bind<IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>>>().To<ResolversBuilder>()
        .Bind<IBuilder<ContractsBuildContext, ISet<Injection>>>().To<ContractsBuilder>()
        .Bind<IBuilder<CompositionCode, LinesBuilder>>().To<ClassDiagramBuilder>()
        .Bind<IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>>>().To<RootsBuilder>()
        .Bind<IBuilder<MdSetup, IEnumerable<DependencyNode>>>(typeof(FactoryDependencyNodeBuilder)).To<FactoryDependencyNodeBuilder>()
        .Bind<IBuilder<MdSetup, IEnumerable<DependencyNode>>>(typeof(ArgDependencyNodeBuilder)).To<ArgDependencyNodeBuilder>()
        .Bind<IBuilder<MdSetup, IEnumerable<DependencyNode>>>(typeof(ConstructDependencyNodeBuilder)).To<ConstructDependencyNodeBuilder>()
        .Bind<IBuilder<MdSetup, IEnumerable<DependencyNode>>>(typeof(ImplementationDependencyNodeBuilder)).To<ImplementationDependencyNodeBuilder>()
        .Bind<IBuilder<MdSetup, IEnumerable<DependencyNode>>>(typeof(RootDependencyNodeBuilder)).To<RootDependencyNodeBuilder>()
        .Bind<IBuilder<MdSetup, DependencyGraph>>().To<VariationalDependencyGraphBuilder>()
        .Bind<IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>>>().To<MetadataBuilder>()
        
        // CSharp
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ClassBuilder).To<ClassBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.DisposeMethodBuilder).To<DisposeMethodBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.RootPropertiesBuilder).To<RootPropertiesBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.UsingDeclarationsBuilder).To<UsingDeclarationsBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ArgFieldsBuilder).To<ArgFieldsBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.SingletonFieldsBuilder).To<SingletonFieldsBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ChildConstructorBuilder).To<ChildConstructorBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.PrimaryConstructorBuilder).To<PrimaryConstructorBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.DefaultConstructorBuilder).To<DefaultConstructorBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ResolverClassesBuilder).To<ResolverClassesBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.StaticConstructorBuilder).To<StaticConstructorBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ApiMembersBuilder).To<ApiMembersBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ResolversFieldsBuilder).To<ResolversFieldsBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ToStringMethodBuilder).To<ToStringMethodBuilder>()

        // Singleton
        .DefaultLifetime(Lifetime.Singleton)
        .Bind<IBuilder<DpImplementation, IEnumerable<DpImplementation>>>().To<ImplementationVariantsBuilder>()
        .Bind<IObserversRegistry>().Bind<IObserversProvider>().To<ObserversRegistry>()
        
        .Root<IObserversRegistry>("ObserversRegistry")
        .Root<IBuilder<IEnumerable<SyntaxUpdate>, Unit>>("Generator");
}