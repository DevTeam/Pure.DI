// ReSharper disable UnusedMember.Local
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// @formatter:off
namespace Pure.DI;

using System.Diagnostics;
using System.Text.RegularExpressions;
using Core.Code.Parts;
using static Hint;
using static Lifetime;
using static RootKinds;
using static Tag;
using Metadata = Core.Metadata;

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
        .Hint(Resolve, "Off")

        .Root<IEnumerable<Source>>(nameof(Api))
        .Root<IObserversRegistry>(nameof(Observers))
        .RootBind<Unit>(nameof(Generate), Internal)
            .To((IBuilder<IEnumerable<SyntaxUpdate>, Unit> generator, IEnumerable<SyntaxUpdate> updates) => generator.Build(updates))

        .RootArg<IGeneratorOptions>("options")
        .RootArg<ISources>("sources")
        .RootArg<IGeneratorDiagnostic>("diagnostic")
        .RootArg<IEnumerable<SyntaxUpdate>>("updates")
        .RootArg<CancellationToken>("cancellationToken")

        .DefaultLifetime(Transient)
            .Bind().To<ApiInvocationProcessor>()
            .Bind().To<DependencyGraphBuilder>()
            .Bind().To<TypeConstructor>()
            .Bind<IEqualityComparer<string>>().To(_ => StringComparer.InvariantCultureIgnoreCase)
            .Bind().To<BindingBuilder>()
            .Bind<ILogger>().To(ctx =>
            {
                ctx.Inject<Logger>(out var logger);
                return logger.WithTargetType(ctx.ConsumerTypes[0]);
            })
            .Bind().To<VariablesMap>()

            // Walkers
            .Bind<IMetadataWalker>().To<MetadataWalker>()
            .Bind<IVariablesWalker>().To<VariablesWalker>()
            .Bind<IInjectionsWalker>().To<InjectionsWalker>()
            .Bind<INamespacesWalker>().To<NamespacesWalker>()
            .Bind<IFactoryResolversWalker>().To<FactoryResolversWalker>()
            .Bind<IInitializersWalker>().To<InitializersWalker>()
            .Bind<IConstructorInjectionsCounterWalker>().To<ConstructorInjectionsCounterWalker>()
            .Bind<IDependencyGraphLocationsWalker>().To<DependencyGraphLocationsWalker>()
            .Bind<IFactoryValidator>().To<FactoryValidator>()
            .Bind<ILocalVariableRenamingRewriter>().To<LocalVariableRenamingRewriter>()

        .DefaultLifetime(Singleton)
            .Bind().To<Cache<TT1, TT2>>()
            .Bind().To<ObserversRegistry>()
            .Bind().To((IBuilder<Unit, IEnumerable<Source>> api) => api.Build(Unit.Shared))
            .Bind().To<Metadata>()
            .Bind().To<Information>()

        .DefaultLifetime(PerBlock)
            .Bind().To(_ => typeof(CodeGenerator).Assembly)
            .Bind().To<Arguments>()
            .Bind().To<Comments>()
            .Bind().To<BuildTools>()
            .Bind().To<Resources>()
            .Bind().To<GlobalProperties>()
            .Bind().To<Marker>()
            .Bind().To<Variator<TT>>()
            .Bind().To<Profiler>()
            .Bind().To<BaseSymbolsProvider>()
            .Bind().To<Formatter>()
            .Bind().To<NodeInfo>()
            .Bind().To<ExceptionHandler>()
            .Bind().To<WildcardMatcher>()
            .Bind().To<InjectionSiteFactory>()
            .Bind().To<Semantic>()
            .Bind().To<Attributes>()
            .Bind().To<Compilations>()
            .Bind().To<GraphWalker<TT, TT1>>()
            .Bind(Type).To<LifetimesValidatorVisitor>()
            .Bind(Type).To<CyclicDependencyValidatorVisitor>()
            .Bind().To<LifetimeAnalyzer>()
            .Bind().To<TriviaTools>()
            .Bind().To<InstanceDpProvider>()
            .Bind().To<Injections>()
            .Bind().To<NameFormatter>()
            .Bind().To(_ => new Func<string, Regex>(
                pattern => new Regex(
                    pattern,
                    RegexOptions.Compiled
                    | RegexOptions.CultureInvariant
                    | RegexOptions.Singleline
                    | RegexOptions.IgnoreCase)))
            .Bind().To(ctx => new Func<DependencyNode, ISet<Injection>, IProcessingNode>((node, contracts) =>
            {
                ctx.Inject(out IInjectionsWalker injectionsWalker);
                return new ProcessingNode(injectionsWalker, node, contracts);
            }))

            // Validators
            .Bind(Type).To<MetadataValidator>()
            .Bind(Type).To<DependencyGraphValidator>()
            .Bind(Type).To<CyclicDependenciesValidator>()
            .Bind(Type).To<RootValidator>()
            .Bind(Type).To<TagOnSitesValidator>()
            .Bind(Type).To<BindingsValidator>()
            .Bind(Type).To<LifetimesValidator>()

            // Comments
            .Bind(Type).To<ClassCommenter>()
            .Bind(Type).To<ParameterizedConstructorCommenter>()
            .Bind(Type).To<RootMethodsCommenter>()

            // Builders
            .Bind().To<MetadataBuilder>()
            .Bind().To<LogInfoBuilder>()
            .Bind().To<ResolversBuilder>()
            .Bind().To<ContractsBuilder>()
            .Bind().To<ClassDiagramBuilder>()
            .Bind().To<RootsBuilder>()
            .Bind(Unique).To<FactoryDependencyNodeBuilder>()
            .Bind(Unique).To<ArgDependencyNodeBuilder>()
            .Bind(Unique).To<ConstructDependencyNodeBuilder>()
            .Bind(Unique).To<ImplementationDependencyNodeBuilder>()
            .Bind(Unique).To<RootDependencyNodeBuilder>()
            .Bind().To<VariationalDependencyGraphBuilder>()
            .Bind().To<ImplementationVariantsBuilder>()
            .Bind().To<ApiBuilder>()
            .Bind().To<CodeBuilder>()
            .Bind().To<SetupsBuilder>()
            .Bind().To<CodeGenerator>()
            .Bind().To<FactoryTypeRewriter>()
            .Bind().To<CompositionBuilder>()
            .Bind().To<TagClassBuilder>()
            .Bind().To<VariablesBuilder>()
            .Bind().To<MermaidUrlBuilder>()

            // Code builders
            .Bind().To<StatementCodeBuilder>()
            .Bind().To<BlockCodeBuilder>()
            .Bind().To<VariableCodeBuilder>()
            .Bind().To<ImplementationCodeBuilder>()
            .Bind().To<FactoryCodeBuilder>()
            .Bind().To<ConstructCodeBuilder>()
            .Bind(CompositionClass).To<CompositionClassBuilder>()
            .Bind(UsingDeclarations).To<UsingDeclarationsBuilder>()
            .Bind(Unique).To<DisposeMethodBuilder>()
            .Bind(Unique).To<RootMethodsBuilder>()
            .Bind(Unique).To<ArgFieldsBuilder>()
            .Bind(Unique).To<FieldsBuilder>()
            .Bind(Unique).To<ScopeConstructorBuilder>()
            .Bind(Unique).To<ParameterizedConstructorBuilder>()
            .Bind(Unique).To<DefaultConstructorBuilder>()
            .Bind(Unique).To<ResolverClassesBuilder>()
            .Bind(Unique).To<StaticConstructorBuilder>()
            .Bind(Unique).To<ApiMembersBuilder>()
            .Bind(Unique).To<ResolversFieldsBuilder>()
            .Bind(Unique).To<ToStringMethodBuilder>()

        .DefaultLifetime(PerResolve)
            .Bind<IReadOnlyCollection<IClassPartBuilder>>()
                .To((IEnumerable<IClassPartBuilder> builders) => builders.OrderBy(i => i.Part).ToList())
            .Bind().To<TypeResolver>()
            .Bind().To<LogObserver>()
            .Bind().To<Types>()
            .Bind().To<Filter>()
            .Bind(UniqueTag).To<IdGenerator>()
            .Bind(GenericType).To<IdGenerator>()
            .Bind(Injection).To<IdGenerator>()
            .Bind().To<IdGenerator>()
            .Bind().To<Registry<TT>>()
            .Bind().To<Locks>()
            .Bind().To<RootAccessModifierResolver>()
            .Bind().To<SmartTags>()
            .Bind().To<GenericTypeArguments>()
            .Bind().To<VariableNameProvider>();}
// @formatter:on