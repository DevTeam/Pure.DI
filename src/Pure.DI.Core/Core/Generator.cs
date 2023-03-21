// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Diagnostics;
using Models;

internal class Generator : IGenerator
{
    private readonly ILogger<Generator> _logger;
    private readonly IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> _metadataBuilder;
    private readonly IBuilder<MdSetup, DependencyGraph> _dependencyGraphBuilder;
    private readonly IValidator<DependencyGraph> _dependencyGraphValidator;
    private readonly IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> _rootsBuilder;
    private readonly IBuilder<DependencyGraph, ComposerCode> _composerBuilder;
    private readonly IBuilder<ComposerCode, ComposerCode> _classBuilder;
    private readonly IContextProducer _contextProducer;
    private readonly IObserversRegistry _observersRegistry;
    private readonly ILogObserver _logObserver;
    private readonly ImmutableArray<IObserver<DependencyGraph>> _dependencyGraphObservers;

    public Generator(
        ILogger<Generator> logger,
        IObserversRegistry observersRegistry,
        ILogObserver logObserver,
        IObserversProvider observersProvider,
        IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> metadataBuilder,
        IBuilder<MdSetup, DependencyGraph> dependencyGraphBuilder,
        IValidator<DependencyGraph> dependencyGraphValidator,
        IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> rootsBuilder,
        [IoC.Tag(WellknownTag.CSharpComposerBuilder)] IBuilder<DependencyGraph, ComposerCode> composerBuilder,
        [IoC.Tag(WellknownTag.CSharpClassBuilder)] IBuilder<ComposerCode, ComposerCode> classBuilder,
        IContextProducer contextProducer)
    {
        _logger = logger;
        _observersRegistry = observersRegistry;
        _logObserver = logObserver;
        _metadataBuilder = metadataBuilder;
        _dependencyGraphBuilder = dependencyGraphBuilder;
        _dependencyGraphValidator = dependencyGraphValidator;
        _rootsBuilder = rootsBuilder;
        _composerBuilder = composerBuilder;
        _classBuilder = classBuilder;
        _contextProducer = contextProducer;
        _dependencyGraphObservers = observersProvider.GetObservers<DependencyGraph>().ToImmutableArray();
    }

    public void Generate(IEnumerable<SyntaxUpdate> updates, CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        using var logObserverToken= _observersRegistry.Register(_logObserver);
        try
        {
            using var logToken = _logger.TraceProcess("generating");
            var setups = _metadataBuilder.Build(updates, cancellationToken);
            foreach (var setup in setups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dependencyGraph = _dependencyGraphBuilder.Build(setup, cancellationToken);
                foreach (var graphObserver in _dependencyGraphObservers)
                {
                    graphObserver.OnNext(dependencyGraph);
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                _dependencyGraphValidator.Validate(dependencyGraph, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                var roots = _rootsBuilder.Build(dependencyGraph, cancellationToken);
                if (!roots.Any())
                {
                    return;
                }
        
                cancellationToken.ThrowIfCancellationRequested();
                var composer = _composerBuilder.Build(dependencyGraph with { Roots = roots }, cancellationToken);
                if (!composer.Roots.Any())
                {
                    return;
                }
                
                cancellationToken.ThrowIfCancellationRequested();
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
                    DiagnosticSeverity.Info,
#else
                    DiagnosticSeverity.Hidden,
#endif
                    ImmutableArray.Create("Generation interrupted."),
                    default,
                    LogId.InfoGenerationInterrupted,
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
            stopwatch.Stop();
            var log = _logObserver.Log;
            log.Insert(0, $"/*{Environment.NewLine}");
            log.AppendLine($"Total duration: {stopwatch.Elapsed:c}");
            log.AppendLine("*/");
            _contextProducer.AddSource("PureDI.log", SourceText.From(log.ToString(), Encoding.UTF8));
        }
    }
}