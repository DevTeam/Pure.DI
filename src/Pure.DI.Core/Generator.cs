// ReSharper disable UnusedMember.Local
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// @formatter:off
namespace Pure.DI;

using System.Diagnostics;
using System.Text.RegularExpressions;
using Core.Code;
using Core.Code.Parts;
using static System.Text.RegularExpressions.RegexOptions;
using static CodeBuilderKind;
using static Hint;
using static Name;
using static RootKinds;
using static StringComparer;
using static Tag;
using static Unit;
using Metadata = Core.Metadata;

// @formatter:off
public sealed partial class Generator
{
    public void Generate(
        ParseOptions parseOptions,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        in SourceProductionContext sourceProductionContext,
        in ImmutableArray<GeneratorSyntaxContext> changes,
        CancellationToken cancellationToken) => 
        Generate(
            new GeneratorOptions(parseOptions, analyzerConfigOptionsProvider),
            new ProductionContextToSourcesAdapter(sourceProductionContext),
            new GeneratorDiagnostic(sourceProductionContext),
            changes.Select(change => new SyntaxUpdate(change.Node, change.SemanticModel)),
            cancellationToken);

    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Resolve, Off)

        .Root<IEnumerable<Source>>(nameof(Api))
        .Root<IObserversRegistry>(nameof(Observers))
        .RootBind<Unit>(nameof(Generate), Internal)
            .To((IBuilder<IEnumerable<SyntaxUpdate>, Unit> generator, IEnumerable<SyntaxUpdate> updates) => generator.Build(updates))

        .RootArg<IGeneratorOptions>(options)
        .RootArg<ISources>(sources)
        .RootArg<IGeneratorDiagnostic>(diagnostic)
        .RootArg<IEnumerable<SyntaxUpdate>>(updates)
        .RootArg<CancellationToken>(cancellationToken)

         // Transient
            .Transient(_ => GetType().Assembly)
            .Transient<IEqualityComparer<string>>(_ => InvariantCultureIgnoreCase)
            .Transient(ctx =>
            {
                ctx.Inject<IObserversProvider>(out var observersProvider);
                return new Logger(observersProvider, ctx.ConsumerType);
            })
            .Transient(_ => Compiled | CultureInvariant | Singleline | IgnoreCase)
            .Transient((RegexOptions options) => new Func<string, Regex>(p => new Regex(p, options)))
            .Transient<ApiInvocationProcessor, DependencyGraphBuilder, TypeConstructor, BindingBuilder, SetupContextRewriter, SetupContextMembersCollector>()
            .Transient<LocalCache<TT1, TT2>>(LocalCache)

            // Walkers
            .SpecialType<CSharpSyntaxRewriter>()
            .SpecialType<CSharpSyntaxWalker>()
            .Transient<MetadataWalker, InjectionsWalker, FactoryApiWalker, ConstructorInjectionsCounterWalker, DependencyGraphLocationsWalker, FactoryValidator,
                LocalVariableRenamingRewriter, VarsMap, VariablesWalker, InitializersWalker, FactoryRewriter>()

        // Singleton
            .Singleton((IBuilder<Unit, IEnumerable<Source>> api) => api.Build(Shared))
            .Singleton<Cache<TT1, TT2>, ObserversRegistry, Metadata, Information, VariableTools, UniqueNameProvider>()

        // PerBlock
            .PerBlock<Arguments, Comments, BuildTools, Resources, GlobalProperties, Marker, Variator<TT>, Profiler, BaseSymbolsProvider, Formatter,
                NodeTools, LocalFunctions, ExceptionHandler, WildcardMatcher, InjectionSiteFactory, Semantic, Attributes, Compilations, GraphWalker<TT, TT1>,
                LifetimeAnalyzer, InstanceDpProvider, Injections, NameFormatter, BindingsFactory, NodesFactory, LocationProvider,
                CycleTools, LifetimeProvider, VarDeclarationTools, ContractTagComparer,
                CodeNameProvider, DependencyNodePrioritizer>()
            .PerBlock<LifetimesValidatorVisitor, CyclicDependencyValidatorVisitor, RootArgsVisitor>()
            .PerBlock<GraphOverrider>(Overrider)
            .PerBlock<GraphCleaner>(Cleaner)

            // Validators
            .PerBlock<MetadataValidator, DependsOnInstanceMemberValidator, DependencyGraphValidator, CyclicDependenciesValidator, RootValidator, TagOnSitesValidator, BindingsValidator, LifetimesValidator>(Type)

            // Comments
            .PerBlock<ClassCommenter, ParameterizedConstructorCommenter, RootMethodsCommenter>(Type)

            // Builders
            .PerBlock<MetadataBuilder, LogInfoBuilder, ResolversBuilder, LightweightRootClassBuilder, ContractsBuilder, ClassDiagramBuilder, RootsBuilder, VariationalDependencyGraphBuilder,
                ImplementationVariantsBuilder, ApiBuilder, CodeBuilder, SetupsBuilder, CodeGenerator, FactoryTypeRewriter, TagClassBuilder, MermaidUrlBuilder,
                CompositionBuilder, RootBuilder, RootCodeBuilder, ProcessingNodeBuilder>()
            .PerBlock<FactoryDependencyNodeBuilder, ArgDependencyNodeBuilder, ConstructDependencyNodeBuilder, ImplementationDependencyNodeBuilder, RootDependencyNodeBuilder>(Unique)
            .PerBlock<ImplementationCodeBuilder>(Implementation)
            .PerBlock<FactoryCodeBuilder>(Factory)
            .PerBlock<ConstructCodeBuilder>(Construct)
            .PerBlock<EnumerableCodeBuilder>(Enumerable, AsyncEnumerable)
            .PerBlock<ArrayCodeBuilder>(Array)
            .PerBlock<SpanCodeBuilder>(Span)
            .PerBlock<CompositionCodeBuilder>(Composition)
            .PerBlock<OnCannotResolveCodeBuilder>(CannotResolve)
            .PerBlock<ExplicitDefaultValueCodeBuilder>(ExplicitDefaultValue)

            // Code builders
            .PerBlock<CompositionClassBuilder>(CompositionClass)
            .PerBlock<UsingDeclarationsBuilder>(UsingDeclarations)
            .PerBlock<DisposeMethodBuilder, RootMethodsBuilder, ArgFieldsBuilder, SetupContextMembersBuilder, FieldsBuilder, ScopeConstructorBuilder, ParameterizedConstructorBuilder, DefaultConstructorBuilder,
                ResolverClassesBuilder, StaticConstructorBuilder, ApiMembersBuilder, ResolversFieldsBuilder, ToStringMethodBuilder>(Unique)

        // PerResolve
            .PerResolve<TypeResolver, RootSignatureProvider, LogObserver, Types, Filter, Registry<TT>, Locks, RootAccessModifierResolver, SmartTags, GenericTypeArguments,
                NameProvider, OverrideIdProvider, OverridesRegistry, Accumulators, Constructors>()

            // Id generators
            .PerResolve<IdGenerator>(UniqueTagIdGenerator)
            .PerResolve<IdGenerator>(OverridesIdGenerator)
            .PerResolve<IdGenerator>(SpecialBindingIdGenerator)
            .PerResolve<IdGenerator>(VarNameIdGenerator);
}
// @formatter:
