// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.Diagnostics;

internal sealed class Generator : IBuilder<IEnumerable<SyntaxUpdate>, Unit>
{
    private readonly ILogger<Generator> _logger;
    private readonly IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> _metadataBuilder;
    private readonly IBuilder<MdSetup, DependencyGraph> _dependencyGraphBuilder;
    private readonly IValidator<DependencyGraph> _dependencyGraphValidator;
    private readonly IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> _rootsBuilder;
    private readonly IBuilder<DependencyGraph, CompositionCode> _compositionBuilder;
    private readonly IBuilder<CompositionCode, CompositionCode> _classBuilder;
    private readonly IValidator<MdSetup> _metadataValidator;
    private readonly ISourcesRegistry _sourcesRegistry;
    private readonly CancellationToken _cancellationToken;
    private readonly IObserversRegistry _observersRegistry;
    private readonly IObserver<LogEntry> _logObserver;
    private readonly IObserversProvider _observersProvider;

    public Generator(
        ILogger<Generator> logger,
        IObserversRegistry observersRegistry,
        IObserver<LogEntry> logObserver,
        IObserversProvider observersProvider,
        IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> metadataBuilder,
        IBuilder<MdSetup, DependencyGraph> dependencyGraphBuilder,
        IValidator<DependencyGraph> dependencyGraphValidator,
        IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> rootsBuilder,
        [Tag(WellknownTag.CompositionBuilder)] IBuilder<DependencyGraph, CompositionCode> compositionBuilder,
        [Tag(WellknownTag.ClassBuilder)] IBuilder<CompositionCode, CompositionCode> classBuilder,
        IValidator<MdSetup> metadataValidator,
        ISourcesRegistry sourcesRegistry,
        CancellationToken cancellationToken)
    {
        _logger = logger;
        _observersRegistry = observersRegistry;
        _logObserver = logObserver;
        _observersProvider = observersProvider;
        _metadataBuilder = metadataBuilder;
        _dependencyGraphBuilder = dependencyGraphBuilder;
        _dependencyGraphValidator = dependencyGraphValidator;
        _rootsBuilder = rootsBuilder;
        _compositionBuilder = compositionBuilder;
        _classBuilder = classBuilder;
        _metadataValidator = metadataValidator;
        _sourcesRegistry = sourcesRegistry;
        _cancellationToken = cancellationToken;
    }

    public Unit Build(IEnumerable<SyntaxUpdate> updates)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        using var logObserverToken= _observersRegistry.Register(_logObserver);
        try
        {
            foreach (var setup in _metadataBuilder.Build(updates))
            {
                try
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
                    _sourcesRegistry.AddSource($"{setup.Name.FullName}.g.cs", SourceText.From(classCode, Encoding.UTF8));
                }
                catch (HandledException handledException)
                {
                    OnHandledException(handledException);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (CompileErrorException compileError)
        {
            _logger.CompileError(compileError.ErrorMessage, compileError.Location, compileError.Id);
        }
        catch (HandledException handledException)
        {
            OnHandledException(handledException);
        }
        catch (Exception error)
        {
            _logger.Log(
                new LogEntry(
                    DiagnosticSeverity.Error,
                    "An unhandled error has occurred.",
                    default,
                    LogId.ErrorUnhandled,
                    error));
        }
        finally
        {
            stopwatch.Stop();
            _logObserver.OnCompleted();
        }
        
        return Unit.Shared;
    }

    private void OnHandledException(Exception handledException)
    {
        _logger.Log(
            new LogEntry(
#if DEBUG
                DiagnosticSeverity.Info,
#else
                DiagnosticSeverity.Hidden,
#endif
                "Generation interrupted.",
                default,
                LogId.InfoGenerationInterrupted,
                handledException));
    }
}