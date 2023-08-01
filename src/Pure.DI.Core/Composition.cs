// ReSharper disable UnusedMember.Local
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI;

using Core;
using Core.CSharp;
using Core.Models;

// ReSharper disable once PartialTypeWithSinglePart
internal partial class Composition
{
    private static void Setup() => DI.Setup(nameof(Composition))
        .DependsOn(nameof(CompositionBase))
        .Arg<IOptions>("options")
        .Arg<ISourcesRegistry>("sources")
        .Arg<IDiagnostic>("diagnostic")

        // Transient
        .DefaultLifetime(Lifetime.Transient)
        .Bind<IMetadataSyntaxWalker>().To<MetadataSyntaxWalker>()
        .Bind<IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>>().To<SetupsBuilder>()
        .Bind<IBuilder<IEnumerable<SyntaxUpdate>, Unit>>().To<Core.Generator>()
        .Bind<IDependencyGraphBuilder>().To<DependencyGraphBuilder>()
        .Bind<IVarIdGenerator>().To<VarIdGenerator>()
        .Bind<ITypeConstructor>().To<TypeConstructor>()
        .Bind<IBuilder<RewriterContext<MdFactory>, MdFactory>>().To<FactoryTypeRewriter>()
        .Bind<IBuilder<DependencyGraph, CompositionCode>>(WellknownTag.CSharpCompositionBuilder).To<CompositionBuilder>()

        // PerResolve
        .DefaultLifetime(Lifetime.PerResolve)
        .Bind<IInformation>().To<Information>()
        .Bind<IClock>().To<Clock>()
        .Bind<ILogger<TT>>().To<Logger<TT>>()
        .Bind<ILogObserver>().To<LogObserver>()
        .Bind<IGlobalOptions>().To<GlobalOptions>()
        .Bind<IFormatting>().To<Formatting>()
        .Bind<IMarker>().To<Marker>()
        .Bind<IFilter>().To<Filter>()
        .Bind<IVariator<TT>>().To<Variator<TT>>()
        .Bind<IUnboundTypeConstructor>().To<UnboundTypeConstructor>()
        .Bind<IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>>>().To<ResolversBuilder>()
        .Bind<IEqualityComparer<string>>().To(_ => StringComparer.InvariantCultureIgnoreCase)
        .Bind<Func<ImmutableArray<byte>, bool>>().To(_ => new Func<ImmutableArray<byte>, bool>(_ => true))
        .Bind<IBuilder<LogEntry, LogInfo>>().To<LogInfoBuilder>()
        .Bind<IValidator<DependencyGraph>>().To<DependencyGraphValidator>()
        .Bind<IValidator<MdSetup>>().To<MetadataValidator>()
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
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpClassBuilder).To<ClassBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpDisposeMethodBuilder).To<DisposeMethodBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpRootPropertiesBuilder).To<RootPropertiesBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpUsingDeclarationsBuilder).To<UsingDeclarationsBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpArgFieldsBuilder).To<ArgFieldsBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpSingletonFieldsBuilder).To<SingletonFieldsBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpChildConstructorBuilder).To<ChildConstructorBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpPrimaryConstructorBuilder).To<PrimaryConstructorBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpDefaultConstructorBuilder).To<DefaultConstructorBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpResolverClassesBuilder).To<ResolverClassesBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpStaticConstructorBuilder).To<StaticConstructorBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpApiMembersBuilder).To<ApiMembersBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpResolversFieldsBuilder).To<ResolversFieldsBuilder>()
        .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.CSharpToStringBuilder).To<ToStringBuilder>()

        // Singleton
        .DefaultLifetime(Lifetime.Singleton)
        .Bind<IBuilder<DpImplementation, IEnumerable<DpImplementation>>>().To<ImplementationVariantsBuilder>()
        .Bind<IObserversRegistry>().Bind<IObserversProvider>().To<ObserversRegistry>()
        
        .Root<IObserversRegistry>("ObserversRegistry")
        .Root<IBuilder<IEnumerable<SyntaxUpdate>, Unit>>("Generator");
}