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
    private readonly IBuilder<DependencyGraph, CompositionCode> _compositionBuilder;
    private readonly IBuilder<CompositionCode, CompositionCode> _classBuilder;
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
        [IoC.Tag(WellknownTag.CSharpCompositionBuilder)] IBuilder<DependencyGraph, CompositionCode> compositionBuilder,
        [IoC.Tag(WellknownTag.CSharpClassBuilder)] IBuilder<CompositionCode, CompositionCode> classBuilder,
        IContextProducer contextProducer)
    {
        _logger = logger;
        _observersRegistry = observersRegistry;
        _logObserver = logObserver;
        _metadataBuilder = metadataBuilder;
        _dependencyGraphBuilder = dependencyGraphBuilder;
        _dependencyGraphValidator = dependencyGraphValidator;
        _rootsBuilder = rootsBuilder;
        _compositionBuilder = compositionBuilder;
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
                using var setupToken = _logger.TraceProcess($"processing setup {setup.TypeName}");

                DependencyGraph dependencyGraph;
                using (_logger.TraceProcess($"building dependency graph {setup.TypeName}"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    dependencyGraph = _dependencyGraphBuilder.Build(setup, cancellationToken);
                    foreach (var graphObserver in _dependencyGraphObservers)
                    {
                        graphObserver.OnNext(dependencyGraph);
                    }
                }

                using (_logger.TraceProcess($"validating dependency graph {setup.TypeName}"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _dependencyGraphValidator.Validate(dependencyGraph, cancellationToken);
                }

                IReadOnlyDictionary<Injection, Root> roots;
                using (_logger.TraceProcess($"search for roots {setup.TypeName}"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    roots = _rootsBuilder.Build(dependencyGraph, cancellationToken);
                    if (!roots.Any())
                    {
                        return;
                    }
                }

                CompositionCode composition;
                using (_logger.TraceProcess($"building composition {setup.TypeName}"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    composition = _compositionBuilder.Build(dependencyGraph with { Roots = roots }, cancellationToken);
                    if (!composition.Roots.Any())
                    {
                        return;
                    }
                }

                using (_logger.TraceProcess($"generating composition code {setup.TypeName}"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    composition = _classBuilder.Build(composition, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                var classCode = string.Join(Environment.NewLine, composition.Code);
                _contextProducer.AddSource($"{setup.TypeName}.g.cs", SourceText.From(classCode, Encoding.UTF8));
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
            log.Append(_logObserver.Outcome);
            log.AppendLine($"Done in {stopwatch.Elapsed.TotalMilliseconds:F} ms");
            log.AppendLine("*/");
            _contextProducer.AddSource("PureDI.log.g.cs", SourceText.From(log.ToString(), Encoding.UTF8));
        }
    }
}