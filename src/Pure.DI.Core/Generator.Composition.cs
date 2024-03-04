// ReSharper disable UnusedMember.Local
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI;

using System.Diagnostics;
using static Lifetime;

// ReSharper disable once PartialTypeWithSinglePart
public partial class Generator
{
    [Conditional("DI")]
    private static void Setup() => DI.Setup(nameof(Generator))
        .Hint(Hint.Resolve, "Off")

        .RootArg<IGeneratorOptions>("options")
        .RootArg<IGeneratorSources>("sources")
        .RootArg<IGeneratorDiagnostic>("diagnostic")
        .RootArg<CancellationToken>("cancellationToken")
        
        .Root<IEnumerable<Source>>("Api")
        .Root<IObserversRegistry>("Observers")
        .Root<IBuilder<IEnumerable<SyntaxUpdate>, Unit>>("CreateGenerator", default, RootKinds.Private)
        
        // Transient
            .Bind<IApiInvocationProcessor>().To<ApiInvocationProcessor>()
            .Bind<IMetadataSyntaxWalker>().To<MetadataSyntaxWalker>()
            .Bind<IDependencyGraphBuilder>().To<DependencyGraphBuilder>()
            .Bind<ITypeConstructor>().To<TypeConstructor>()
            .Bind<IEqualityComparer<string>>().To(_ => StringComparer.InvariantCultureIgnoreCase)
        
        // Singleton
            .DefaultLifetime(Singleton)
            .Bind<ICache<TT1, TT2>>().To<Cache<TT1, TT2>>()
            .Bind<IObserversRegistry>().Bind<IObserversProvider>().To<ObserversRegistry>()
            .Bind<IEnumerable<Source>>().To(ctx =>
            {
                ctx.Inject<IBuilder<Unit, IEnumerable<Source>>>(out var api);
                return api.Build(Unit.Shared);
            })
        
        // PerBlock
            .DefaultLifetime(PerBlock)
            .Bind<IArguments>().To<Arguments>()
            .Bind<IComments>().To<Comments>()
            .Bind<IBuilder<IEnumerable<string>, Uri>>().To<MermaidUrlBuilder>()
            .Bind<IBuildTools>().To<BuildTools>()
            .Bind<ILogger<TT>>().To<Logger<TT>>()
            .Bind<IResources>().To<Resources>()
            .Bind<IInformation>().To<Information>()
            .Bind<IGlobalOptions>().To<GlobalOptions>()
            .Bind<Func<ImmutableArray<byte>, bool>>().To(_ => new Func<ImmutableArray<byte>, bool>(_ => true))
            .Bind<IValidator<DependencyGraph>>().To<DependencyGraphValidator>()
            .Bind<IValidator<MdSetup>>().To<MetadataValidator>()
            .Bind<IMarker>().To<Marker>()
            .Bind<IVariator<TT>>().To<Variator<TT>>()
            .Bind<IProfiler>().To<Profiler>()
        
            // Commenters
            .Bind<ICommenter<Unit>>(typeof(ClassCommenter)).To<ClassCommenter>()
            .Bind<ICommenter<Unit>>(typeof(ParameterizedConstructorCommenter)).To<ParameterizedConstructorCommenter>()
            .Bind<ICommenter<Root>>(typeof(RootMethodsCommenter)).To<RootMethodsCommenter>()
        
            // Builders
            .Bind<IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>>>().To<MetadataBuilder>()
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
            .Bind<IBuilder<MdSetup, DependencyGraph?>>().To<VariationalDependencyGraphBuilder>()
            .Bind<IBuilder<DpImplementation, IEnumerable<DpImplementation>>>().To<ImplementationVariantsBuilder>()
            .Bind<IBuilder<Unit, IEnumerable<Source>>>().To<ApiBuilder>()
            .Bind<IBuilder<MdSetup, Unit>>().To<CodeBuilder>()
            .Bind<IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>>().To<SetupsBuilder>()
            .Bind<IBuilder<IEnumerable<SyntaxUpdate>, Unit>>().To<Core.Generator>()
            .Bind<IBuilder<RewriterContext<MdFactory>, MdFactory>>().To<FactoryTypeRewriter>()
            .Bind<IBuilder<DependencyGraph, CompositionCode>>(WellknownTag.CompositionBuilder).To<CompositionBuilder>()
            .Bind<IVariablesBuilder>().To<VariablesBuilder>()
            
            // Code builders
            .Bind<ICodeBuilder<IStatement>>().To<StatementCodeBuilder>()
            .Bind<ICodeBuilder<Block>>().To<BlockCodeBuilder>()
            .Bind<ICodeBuilder<Variable>>().To<VariableCodeBuilder>()
            .Bind<ICodeBuilder<DpImplementation>>().To<ImplementationCodeBuilder>()
            .Bind<ICodeBuilder<DpFactory>>().To<FactoryCodeBuilder>()
            .Bind<ICodeBuilder<DpConstruct>>().To<ConstructCodeBuilder>()
            
            // Code
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ClassBuilder).To<ClassBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.DisposeMethodBuilder).To<DisposeMethodBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.RootMethodsBuilder).To<RootMethodsBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.UsingDeclarationsBuilder).To<UsingDeclarationsBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ArgFieldsBuilder).To<ArgFieldsBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.FieldsBuilder).To<FieldsBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ScopeConstructorBuilder).To<ScopeConstructorBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ParameterizedConstructorBuilder).To<ParameterizedConstructorBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.DefaultConstructorBuilder).To<DefaultConstructorBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ResolverClassesBuilder).To<ResolverClassesBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.StaticConstructorBuilder).To<StaticConstructorBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ApiMembersBuilder).To<ApiMembersBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ResolversFieldsBuilder).To<ResolversFieldsBuilder>()
            .Bind<IBuilder<CompositionCode, CompositionCode>>(WellknownTag.ToStringMethodBuilder).To<ToStringMethodBuilder>()
        
        // PerResolve
            .DefaultLifetime(PerResolve)
            .Bind<IObserver<LogEntry>>().To<LogObserver>()
            .Bind<IFilter>().To<Filter>()
            .Bind<IIdGenerator>().To<IdGenerator>();
}