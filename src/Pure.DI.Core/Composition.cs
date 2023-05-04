// ReSharper disable UnusedMember.Local
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI;

using System.Text.RegularExpressions;
using Core;
using Core.CSharp;
using Core.Models;

// ReSharper disable once PartialTypeWithSinglePart
internal static partial class Composition
{
    private static void Setup() => IoC.DI.Setup()
        // Transients
        .Bind<IGlobalOptions>().To<GlobalOptions>()
        .Bind<ILogger<TT>>().To<Logger<TT>>()
        .Bind<ILogObserver>().To<LogObserver>()
        .Bind<IMetadataSyntaxWalker>().To<MetadataSyntaxWalker>()
        .Bind<IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>>().To<SetupsBuilder>()
        .Bind<IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>>>().To<MetadataBuilder>()
        .Bind<IGenerator>().To<Generator>()
        .Bind<IBuilder<Unit, IEnumerable<Source>>>().To<ApiBuilder>()
        .Bind<IDependencyGraphBuilder>().To<DependencyGraphBuilder>()
        .Bind<IBuilder<MdSetup, DependencyGraph>>().To<VariationalDependencyGraphBuilder>()
        .Bind<IBuilder<MdSetup, IEnumerable<DependencyNode>>>(typeof(RootDependencyNodeBuilder)).To<RootDependencyNodeBuilder>()
        .Bind<IBuilder<MdSetup, IEnumerable<DependencyNode>>>(typeof(ImplementationDependencyNodeBuilder)).To<ImplementationDependencyNodeBuilder>()
        .Bind<IBuilder<MdSetup, IEnumerable<DependencyNode>>>(typeof(FactoryDependencyNodeBuilder)).To<FactoryDependencyNodeBuilder>()
        .Bind<IBuilder<MdSetup, IEnumerable<DependencyNode>>>(typeof(ArgDependencyNodeBuilder)).To<ArgDependencyNodeBuilder>()
        .Bind<IBuilder<MdSetup, IEnumerable<DependencyNode>>>(typeof(ConstructDependencyNodeBuilder)).To<ConstructDependencyNodeBuilder>()
        .Bind<IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>>>().To<RootsBuilder>()
        .Bind<IBuilder<CompositionCode, LinesBuilder>>().To<ClassDiagramBuilder>()
        .Bind<IVarIdGenerator>().To<VarIdGenerator>()
        .Bind<IBuilder<ContractsBuildContext, ISet<Injection>>>().To<ContractsBuilder>()
        .Bind<ITypeConstructor>().To<TypeConstructor>()
        .Bind<IBuilder<RewriterContext<MdFactory>, MdFactory>>().To<FactoryTypeRewriter>()
        .Bind<IValidator<MdSetup>>().To<MetadataValidator>()
        .Bind<IValidator<DependencyGraph>>().To<DependencyGraphValidator>()
        .Bind<IBuilder<LogEntry, LogInfo>>().To<LogInfoBuilder>()

        // CSharp
        .Bind<IBuilder<DependencyGraph, CompositionCode>>(WellknownTag.CSharpCompositionBuilder).To<CompositionBuilder>()
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

        .Bind<Facade>().To<Facade>()
        .Arg<IContextOptions>()
        .Arg<IContextProducer>()
        .Arg<IContextDiagnostic>()

        // Singletons
        .Default(IoC.Lifetime.Singleton)
        .Bind<IObserversRegistry>().Bind<IObserversProvider>().To<ObserversRegistry>()
        .Bind<IClock>().To<Clock>()
        .Bind<IFormatting>().To<Formatting>()
        .Bind<ICache<IoC.TT1, IoC.TT2>>().To<Cache<IoC.TT1, IoC.TT2>>()
        .Bind<IResources>().To<Resources>()
        .Bind<IMarker>().To<Marker>()
        .Bind<IFilter>().To<Filter>()
        .Bind<IVariator<TT>>().To<Variator<TT>>()
        .Bind<IBuilder<DpImplementation, IEnumerable<DpImplementation>>>().To<ImplementationVariantsBuilder>()
        .Bind<IUnboundTypeConstructor>().To<UnboundTypeConstructor>()
        .Bind<IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>>>().To<ResolversBuilder>()
        .Bind<IEqualityComparer<string>>().To(_ => StringComparer.InvariantCultureIgnoreCase)
        .Bind<Func<string, Regex>>().To(_ => new Func<string, Regex>(value => new Regex(value, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase)))
        .Bind<IEqualityComparer<ImmutableArray<byte>>>().To<BytesArrayEqualityComparer>()
        .Bind<Func<ImmutableArray<byte>, bool>>().To(_ => new Func<ImmutableArray<byte>, bool>(_ => true));
}