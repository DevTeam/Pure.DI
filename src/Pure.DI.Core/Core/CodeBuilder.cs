// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class CodeBuilder: IBuilder<MdSetup, Unit>
{
    private readonly IBuilder<MdSetup, DependencyGraph> _dependencyGraphBuilder;
    private readonly IValidator<DependencyGraph> _dependencyGraphValidator;
    private readonly IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> _rootsBuilder;
    private readonly IBuilder<DependencyGraph, CompositionCode> _compositionBuilder;
    private readonly IBuilder<CompositionCode, CompositionCode> _classBuilder;
    private readonly IBuilder<MdSetup, SetupClassAttribute> _setupClassAttributeBuilder;
    private readonly IValidator<MdSetup> _metadataValidator;
    private readonly IGeneratorSources _sources;
    private readonly CancellationToken _cancellationToken;
    private readonly IObserversProvider _observersProvider;
    private readonly HashSet<CompositionName> _compositionNames = new();

    public CodeBuilder(
        IObserversProvider observersProvider,
        IBuilder<MdSetup, DependencyGraph> dependencyGraphBuilder,
        IValidator<DependencyGraph> dependencyGraphValidator,
        IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> rootsBuilder,
        [Tag(WellknownTag.CompositionBuilder)] IBuilder<DependencyGraph, CompositionCode> compositionBuilder,
        [Tag(WellknownTag.ClassBuilder)] IBuilder<CompositionCode, CompositionCode> classBuilder,
        IBuilder<MdSetup, SetupClassAttribute> setupClassAttributeBuilder,
        IValidator<MdSetup> metadataValidator,
        IGeneratorSources sources,
        CancellationToken cancellationToken)
    {
        _observersProvider = observersProvider;
        _dependencyGraphBuilder = dependencyGraphBuilder;
        _dependencyGraphValidator = dependencyGraphValidator;
        _rootsBuilder = rootsBuilder;
        _compositionBuilder = compositionBuilder;
        _classBuilder = classBuilder;
        _setupClassAttributeBuilder = setupClassAttributeBuilder;
        _metadataValidator = metadataValidator;
        _sources = sources;
        _cancellationToken = cancellationToken;
    }
    
    public Unit Build(MdSetup setup)
    {
        if (!_metadataValidator.Validate(setup))
        {
            return Unit.Shared;
        }
        
        _cancellationToken.ThrowIfCancellationRequested();
        switch (setup.Kind)
        {
            case CompositionKind.Public:
                BuildPublic(setup);
                break;
            
            case CompositionKind.External:
                BuildExternal(setup);
                break;
        }
        
        return Unit.Shared;
    }

    private void BuildPublic(MdSetup setup)
    {
        var dependencyGraph = _dependencyGraphBuilder.Build(setup);
        foreach (var graphObserver in _observersProvider.GetObservers<DependencyGraph>())
        {
            graphObserver.OnNext(dependencyGraph);
        }

        _cancellationToken.ThrowIfCancellationRequested();
        var roots = _rootsBuilder.Build(dependencyGraph);
        dependencyGraph = dependencyGraph with { Roots = roots };

        _cancellationToken.ThrowIfCancellationRequested();
        if (!_dependencyGraphValidator.Validate(dependencyGraph))
        {
            return;
        }

        _cancellationToken.ThrowIfCancellationRequested();
        var composition = _compositionBuilder.Build(dependencyGraph);

        _cancellationToken.ThrowIfCancellationRequested();
        composition = _classBuilder.Build(composition);

        _cancellationToken.ThrowIfCancellationRequested();
        var classCode = string.Join(Environment.NewLine, composition.Code);
        _sources.AddSource($"{setup.Name.FullName}.g.cs", SourceText.From(classCode, Encoding.UTF8));
    }

    private void BuildExternal(MdSetup setup)
    {
        if (setup.Name.ClassName == "E2FF47DE-7FD9-4628-8862-839094FCEB46")
        {
            return;
        }
        
        if (!_compositionNames.Add(setup.Name))
        {
            return;
        }
        
        if (!setup.Dependencies.IsDefaultOrEmpty)
        {
            foreach (var setupDependency in setup.Dependencies)
            {
                BuildExternal(setupDependency);
            }
        }

        var setupClassAttribute = _setupClassAttributeBuilder.Build(setup);
        var classCode = string.Join(Environment.NewLine, setupClassAttribute.Code);
        _sources.AddSource($"{setup.Name.FullName}.g.cs", SourceText.From(classCode, Encoding.UTF8));
    }
}