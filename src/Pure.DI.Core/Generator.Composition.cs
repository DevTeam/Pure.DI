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
        
        // Roots
            .Root<IEnumerable<Source>>("Api")
            .Root<IObserversRegistry>("Observers")
            .Root<IBuilder<IEnumerable<SyntaxUpdate>, Unit>>("CreateGenerator", kind: RootKinds.Private)

            .RootArg<IGeneratorOptions>("options")
            .RootArg<IGeneratorSources>("sources")
            .RootArg<IGeneratorDiagnostic>("diagnostic")
            .RootArg<CancellationToken>("cancellationToken")
        
        // Transient
            .Bind().To<ApiInvocationProcessor>()
            .Bind().To<MetadataSyntaxWalker>()
            .Bind().To<DependencyGraphBuilder>()
            .Bind().To<TypeConstructor>()
            .Bind<IEqualityComparer<string>>().To(_ => StringComparer.InvariantCultureIgnoreCase)
            .Bind().To<BindingBuilder>()
        
        // Singleton
            .DefaultLifetime(Singleton)
            .Bind().To<Cache<TT1, TT2>>()
            .Bind().To<ObserversRegistry>()
            .Bind().To(ctx =>
            {
                ctx.Inject<IBuilder<Unit, IEnumerable<Source>>>(out var api);
                return api.Build(Unit.Shared);
            })
        
        // PerBlock
            .DefaultLifetime(PerBlock)
            .Bind().To<Arguments>()
            .Bind().To<Comments>()
            .Bind().To<BuildTools>()
            .Bind().To<Logger<TT>>()
            .Bind().To<Resources>()
            .Bind().To<Information>()
            .Bind().To<GlobalOptions>()
            .Bind().To<Marker>()
            .Bind().To<Variator<TT>>()
            .Bind().To<Profiler>()
            .Bind().To<BaseSymbolsProvider>()
            .Bind().To<Formatter>()
        
            // Validators
            .Bind(Tag.Type).To<MetadataValidator>()
            .Bind(Tag.Type).To<DependencyGraphValidator>()
            .Bind(Tag.Type).To<RootValidator>()
        
            // Comments
            .Bind(Tag.Type).To<ClassCommenter>()
            .Bind(Tag.Type).To<ParameterizedConstructorCommenter>()
            .Bind(Tag.Type).To<RootMethodsCommenter>()
        
            // Builders
            .Bind().To<MetadataBuilder>()
            .Bind().To<LogInfoBuilder>()
            .Bind().To<ResolversBuilder>()
            .Bind().To<ContractsBuilder>()
            .Bind().To<ClassDiagramBuilder>()
            .Bind().To<RootsBuilder>()
            .Bind(Tag.Unique).To<FactoryDependencyNodeBuilder>()
            .Bind(Tag.Unique).To<ArgDependencyNodeBuilder>()
            .Bind(Tag.Unique).To<ConstructDependencyNodeBuilder>()
            .Bind(Tag.Unique).To<ImplementationDependencyNodeBuilder>()
            .Bind(Tag.Unique).To<RootDependencyNodeBuilder>()
            .Bind().To<VariationalDependencyGraphBuilder>()
            .Bind().To<ImplementationVariantsBuilder>()
            .Bind().To<ApiBuilder>()
            .Bind().To<CodeBuilder>()
            .Bind().To<SetupsBuilder>()
            .Bind().To<Core.Generator>()
            .Bind().To<FactoryTypeRewriter>()
            .Bind().To<CompositionBuilder>()
            .Bind().To<VariablesBuilder>()
            .Bind().To<MermaidUrlBuilder>()
            
            // Code builders
            .Bind().To<StatementCodeBuilder>()
            .Bind().To<BlockCodeBuilder>()
            .Bind().To<VariableCodeBuilder>()
            .Bind().To<ImplementationCodeBuilder>()
            .Bind().To<FactoryCodeBuilder>()
            .Bind().To<ConstructCodeBuilder>()
            .Bind().To<ClassBuilder>()
            .Bind(Tag.Type).To<DisposeMethodBuilder>()
            .Bind(Tag.Type).To<RootMethodsBuilder>()
            .Bind(Tag.Type).To<UsingDeclarationsBuilder>()
            .Bind(Tag.Type).To<ArgFieldsBuilder>()
            .Bind(Tag.Type).To<FieldsBuilder>()
            .Bind(Tag.Type).To<ScopeConstructorBuilder>()
            .Bind(Tag.Type).To<ParameterizedConstructorBuilder>()
            .Bind(Tag.Type).To<DefaultConstructorBuilder>()
            .Bind(Tag.Type).To<ResolverClassesBuilder>()
            .Bind(Tag.Type).To<StaticConstructorBuilder>()
            .Bind(Tag.Type).To<ApiMembersBuilder>()
            .Bind(Tag.Type).To<ResolversFieldsBuilder>()
            .Bind(Tag.Type).To<ToStringMethodBuilder>()
        
        // PerResolve
            .DefaultLifetime(PerResolve)
            .Bind().To<TypeResolver>()
            .Bind().To<LogObserver>()
            .Bind().To<Filter>()
            .Bind("UniqueTags").To<IdGenerator>()
            .Bind().To<IdGenerator>();
}