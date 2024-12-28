// ReSharper disable UnusedMember.Local
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
namespace Pure.DI;

using System.Diagnostics;
using static Hint;
using static Lifetime;
using static RootKinds;
using static Tag;

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
            new GeneratorSources(sourceProductionContext),
            new GeneratorDiagnostic(sourceProductionContext),
            changes.Select(change => new SyntaxUpdate(change.Node, change.SemanticModel)),
            cancellationToken);
    
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Resolve, "Off")

        // Roots
            .Root<IEnumerable<Source>>(nameof(Api))
            .Root<IObserversRegistry>(nameof(Observers))
            .RootBind<Generation>(nameof(Generate), kind: Internal)
                .To((IBuilder<IEnumerable<SyntaxUpdate>, Generation> generator, IEnumerable<SyntaxUpdate> updates) => generator.Build(updates))

            .RootArg<IGeneratorOptions>("options")
            .RootArg<IGeneratorSources>("sources")
            .RootArg<IGeneratorDiagnostic>("diagnostic")
            .RootArg<IEnumerable<SyntaxUpdate>>("updates")
            .RootArg<CancellationToken>("cancellationToken")

                .Bind().To<ApiInvocationProcessor>()
                .Bind().To<MetadataSyntaxWalker>()
                .Bind().To<DependencyGraphBuilder>()
                .Bind().To<TypeConstructor>()
                .Bind<IEqualityComparer<string>>().To(_ => StringComparer.InvariantCultureIgnoreCase)
                .Bind().To<BindingBuilder>()
                .Bind<ILogger>().To(ctx =>
                {
                    ctx.Inject<Logger>(out var logger);
                    return logger.WithTargetType(ctx.ConsumerTypes[0]);
                })

            .DefaultLifetime(Singleton)
                .Bind().To<Cache<TT1, TT2>>()
                .Bind().To<ObserversRegistry>()
                .Bind().To((IBuilder<Unit, IEnumerable<Source>> api) => api.Build(Unit.Shared))

            .DefaultLifetime(PerBlock)
                .Bind().To<Arguments>()
                .Bind().To<Comments>()
                .Bind().To<BuildTools>()
                .Bind().To<Resources>()
                .Bind().To<Information>()
                .Bind().To<GlobalOptions>()
                .Bind().To<Marker>()
                .Bind().To<Variator<TT>>()
                .Bind().To<Profiler>()
                .Bind().To<BaseSymbolsProvider>()
                .Bind().To<Formatter>()
                .Bind().To<NodeInfo>()
                .Bind<IEqualityComparer<INamedTypeSymbol>>().To<NamedTypeSymbolEqualityComparer>()
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
                .Bind().To<Core.Generator>()
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
                .Bind().To<CompositionClassBuilder>()
                .Bind(Type).To<DisposeMethodBuilder>()
                .Bind(Type).To<RootMethodsBuilder>()
                .Bind(Type).To<UsingDeclarationsBuilder>()
                .Bind(Type).To<ArgFieldsBuilder>()
                .Bind(Type).To<FieldsBuilder>()
                .Bind(Type).To<ScopeConstructorBuilder>()
                .Bind(Type).To<ParameterizedConstructorBuilder>()
                .Bind(Type).To<DefaultConstructorBuilder>()
                .Bind(Type).To<ResolverClassesBuilder>()
                .Bind(Type).To<StaticConstructorBuilder>()
                .Bind(Type).To<ApiMembersBuilder>()
                .Bind(Type).To<ResolversFieldsBuilder>()
                .Bind(Type).To<ToStringMethodBuilder>()

            .DefaultLifetime(PerResolve)
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
                .Bind().To<SmartTags>();
}