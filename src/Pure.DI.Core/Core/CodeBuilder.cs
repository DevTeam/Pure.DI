namespace Pure.DI.Core;

internal class CodeBuilder: IBuilder<MdSetup, Unit>
{
    private readonly IBuilder<MdSetup, DependencyGraph> _dependencyGraphBuilder;
    private readonly IValidator<DependencyGraph> _dependencyGraphValidator;
    private readonly IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> _rootsBuilder;
    private readonly IBuilder<DependencyGraph, CompositionCode> _compositionBuilder;
    private readonly IBuilder<CompositionCode, CompositionCode> _classBuilder;
    private readonly IValidator<MdSetup> _metadataValidator;
    private readonly IGeneratorSources _sources;
    private readonly CancellationToken _cancellationToken;
    private readonly IObserversProvider _observersProvider;

    public CodeBuilder(
        IObserversProvider observersProvider,
        IBuilder<MdSetup, DependencyGraph> dependencyGraphBuilder,
        IValidator<DependencyGraph> dependencyGraphValidator,
        IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> rootsBuilder,
        [Tag(WellknownTag.CompositionBuilder)] IBuilder<DependencyGraph, CompositionCode> compositionBuilder,
        [Tag(WellknownTag.ClassBuilder)] IBuilder<CompositionCode, CompositionCode> classBuilder,
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
        _metadataValidator = metadataValidator;
        _sources = sources;
        _cancellationToken = cancellationToken;
    }
    
    public Unit Build(MdSetup setup)
    {
        _metadataValidator.Validate(setup);
        _cancellationToken.ThrowIfCancellationRequested();
        var dependencyGraph = _dependencyGraphBuilder.Build(setup);
        foreach (var graphObserver in _observersProvider.GetObservers<DependencyGraph>())
        {
            graphObserver.OnNext(dependencyGraph);
        }

        _cancellationToken.ThrowIfCancellationRequested();
        var roots = _rootsBuilder.Build(dependencyGraph);
        dependencyGraph = dependencyGraph with { Roots = roots };

        _cancellationToken.ThrowIfCancellationRequested();
        _dependencyGraphValidator.Validate(dependencyGraph);

        _cancellationToken.ThrowIfCancellationRequested();
        var composition = _compositionBuilder.Build(dependencyGraph);

        _cancellationToken.ThrowIfCancellationRequested();
        composition = _classBuilder.Build(composition);

        _cancellationToken.ThrowIfCancellationRequested();
        var classCode = string.Join(Environment.NewLine, composition.Code);
        _sources.AddSource($"{setup.Name.FullName}.g.cs", SourceText.From(classCode, Encoding.UTF8));
        return Unit.Shared;
    }
}