// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using Models;

internal class Generator : IGenerator
{
    private readonly ILogger<Generator> _logger;
    private readonly IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> _metadataBuilder;
    private readonly IBuilder<MdSetup, DependencyGraph> _dependencyGraphBuilder;
    private readonly IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> _rootsBuilder;
    private readonly IBuilder<DependencyGraph, ComposerInfo> _composerBuilder;
    private readonly IBuilder<ComposerInfo, ComposerInfo> _classBuilder;
    private readonly IContextProducer _contextProducer;
    private readonly IResourceManager _resourceManager;
    private readonly IObserversProvider _observersProvider;

    public Generator(
        ILogger<Generator> logger,
        IResourceManager resourceManager,
        IObserversProvider observersProvider,
        IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> metadataBuilder,
        IBuilder<MdSetup, DependencyGraph> dependencyGraphBuilder,
        IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> rootsBuilder,
        IBuilder<DependencyGraph, ComposerInfo> composerBuilder,
        [IoC.Tag(WellknownTag.ClassBuilder)] IBuilder<ComposerInfo, ComposerInfo> classBuilder,
        IContextProducer contextProducer)
    {
        _logger = logger;
        _resourceManager = resourceManager;
        _observersProvider = observersProvider;
        _metadataBuilder = metadataBuilder;
        _dependencyGraphBuilder = dependencyGraphBuilder;
        _rootsBuilder = rootsBuilder;
        _composerBuilder = composerBuilder;
        _classBuilder = classBuilder;
        _contextProducer = contextProducer;
    }

    public void Generate(IEnumerable<SyntaxUpdate> updates, CancellationToken cancellationToken)
    {
        try
        {
            using var logToken = _logger.TraceProcess("generating");
            var setups = _metadataBuilder.Build(updates, cancellationToken);
            foreach (var setup in setups)
            {
                var dependencyGraph = _dependencyGraphBuilder.Build(setup, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var graphObserver in _observersProvider.GetObservers<DependencyGraph>())
                {
                    graphObserver.OnNext(dependencyGraph);
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                
                var roots = _rootsBuilder.Build(dependencyGraph, cancellationToken);
                if (!roots.Any())
                {
                    return;
                }
        
                var composer = _composerBuilder.Build(dependencyGraph with { Roots = roots }, cancellationToken);
                if (!composer.Roots.Any())
                {
                    return;
                }
                
                composer = _classBuilder.Build(composer, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                var classCode = string.Join(Environment.NewLine, composer.Code);
                _contextProducer.AddSource($"{setup.ComposerTypeName}.g.cs", SourceText.From(classCode, Encoding.UTF8));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (HandledException handledException)
        {
            _logger.Log(
                new LogEntry(
#if DEBUG
                    DiagnosticSeverity.Warning,
#else
                DiagnosticSeverity.Hidden,
#endif
                    ImmutableArray.Create("A generation failure has occurred."),
                    default,
                    LogId.WarningGenerationFailure,
                    handledException));
        }
        catch (Exception error)
        {
            _logger.Log(
                new LogEntry(
                    DiagnosticSeverity.Error,
                    ImmutableArray.Create("An unhandled error has occurred."),
                    default,
                    LogId.ErrorUnhandled,
                    error));

            throw;
        }
        finally
        {
            _resourceManager.Clear();
        }
    }
}