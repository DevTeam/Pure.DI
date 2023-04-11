// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.Diagnostics;
using Models;

internal class Generator : IGenerator
{
    private readonly ILogger<Generator> _logger;
    private readonly IGlobalOptions _globalOptions;
    private readonly IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> _metadataBuilder;
    private readonly IBuilder<MdSetup, DependencyGraph> _dependencyGraphBuilder;
    private readonly IValidator<DependencyGraph> _dependencyGraphValidator;
    private readonly IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> _rootsBuilder;
    private readonly IBuilder<DependencyGraph, CompositionCode> _compositionBuilder;
    private readonly IBuilder<CompositionCode, CompositionCode> _classBuilder;
    private readonly IContextProducer _contextProducer;
    private readonly IObserversRegistry _observersRegistry;
    private readonly ILogObserver _logObserver;
    private readonly IObserversProvider _observersProvider;

    public Generator(
        ILogger<Generator> logger,
        IGlobalOptions globalOptions,
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
        _globalOptions = globalOptions;
        _observersRegistry = observersRegistry;
        _logObserver = logObserver;
        _observersProvider = observersProvider;
        _metadataBuilder = metadataBuilder;
        _dependencyGraphBuilder = dependencyGraphBuilder;
        _dependencyGraphValidator = dependencyGraphValidator;
        _rootsBuilder = rootsBuilder;
        _compositionBuilder = compositionBuilder;
        _classBuilder = classBuilder;
        _contextProducer = contextProducer;
    }

    public void Generate(IEnumerable<SyntaxUpdate> updates, CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var logFile = _globalOptions.LogFile;
        using var logObserverToken= string.IsNullOrWhiteSpace(logFile) ? Disposables.Empty : _observersRegistry.Register(_logObserver);
        try
        {
            using var logToken = _logger.TraceProcess("generation");
            ImmutableArray<MdSetup> setups;
            using (_logger.TraceProcess("metadata analysis"))
            {
                setups = _metadataBuilder.Build(updates, cancellationToken).ToImmutableArray();
            }

            foreach (var setup in setups)
            {
                _logger.Trace(setup.Settings, i => Enumerable.Repeat($"Settings \"{setup.Name.FullName}\":", 1).Concat(i.Select(j => $"{j.Key} = {j.Value}")));
                
                using var setupToken = _logger.TraceProcess($"metadata processing \"{setup.Name.FullName}\", {setup.Bindings.Length} bindings");
                DependencyGraph dependencyGraph;
                using (_logger.TraceProcess($"building a dependency graph \"{setup.Name.FullName}\""))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    dependencyGraph = _dependencyGraphBuilder.Build(setup, cancellationToken);
                    foreach (var graphObserver in _observersProvider.GetObservers<DependencyGraph>())
                    {
                        graphObserver.OnNext(dependencyGraph);
                    }
                }

                using (_logger.TraceProcess($"dependency graph validation \"{setup.Name.FullName}\""))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _dependencyGraphValidator.Validate(dependencyGraph, cancellationToken);
                }

                using (_logger.TraceProcess($"search for roots \"{setup.Name.FullName}\""))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var roots = _rootsBuilder.Build(dependencyGraph, cancellationToken);
                    if (!roots.Any())
                    {
                        return;
                    }
                    
                    dependencyGraph = dependencyGraph with { Roots = roots };
                }

                CompositionCode composition;
                using (_logger.TraceProcess($"creating a composition \"{setup.Name.FullName}\""))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    composition = _compositionBuilder.Build(dependencyGraph, cancellationToken);
                    if (!composition.Roots.Any())
                    {
                        return;
                    }
                }

                using (_logger.TraceProcess($"code generation \"{setup.Name.FullName}\""))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    composition = _classBuilder.Build(composition, cancellationToken);
                }

                using (_logger.TraceProcess($"saving data \"{setup.Name.FullName}\""))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var classCode = string.Join(Environment.NewLine, composition.Code);
                    _contextProducer.AddSource($"{setup.Name.FullName}.g.cs", SourceText.From(classCode, Encoding.UTF8));
                }

                if (_logger.IsTracing())
                {
                    var trace = new List<string> { $"Setup {dependencyGraph.Source.Name.FullName}:", "---------- Map ----------" };
                    foreach (var (injection, dependencyNode) in dependencyGraph.Map)
                    {
                        trace.Add($"{injection} -> {dependencyNode}");
                    }
                    
                    trace.Add("---------- Roots ----------");
                    foreach (var root in dependencyGraph.Roots)
                    {
                        trace.Add($"{root.Key} {root.Value.PropertyName} -> {root.Value.Node}");
                    }
                    
                    _logger.Trace(trace, state => state);
                }
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
            _logObserver.Flush();
            try
            {
                if (!string.IsNullOrWhiteSpace(logFile))
                {
                    var dir = Path.GetDirectoryName(logFile);
                    if (dir is not null)
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var log = _logObserver.Log;
                    log.Append(_logObserver.Outcome);
                    log.AppendLine($"Done in {stopwatch.Elapsed.TotalMilliseconds:F} ms");
                    File.WriteAllText(logFile, log.ToString());
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}