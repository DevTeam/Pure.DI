// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.Diagnostics;

internal sealed class Generator : IBuilder<IEnumerable<SyntaxUpdate>, Unit>
{
    private readonly ILogger<Generator> _logger;
    private readonly IGlobalOptions _globalOptions;
    private readonly IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> _metadataBuilder;
    private readonly IBuilder<MdSetup, DependencyGraph> _dependencyGraphBuilder;
    private readonly IValidator<DependencyGraph> _dependencyGraphValidator;
    private readonly IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> _rootsBuilder;
    private readonly IBuilder<DependencyGraph, CompositionCode> _compositionBuilder;
    private readonly IBuilder<CompositionCode, CompositionCode> _classBuilder;
    private readonly IValidator<MdSetup> _metadataValidator;
    private readonly ISourcesRegistry _sourcesRegistry;
    private readonly IFileSystem _fileSystem;
    private readonly CancellationToken _cancellationToken;
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
        [Tag(WellknownTag.CompositionBuilder)] IBuilder<DependencyGraph, CompositionCode> compositionBuilder,
        [Tag(WellknownTag.ClassBuilder)] IBuilder<CompositionCode, CompositionCode> classBuilder,
        IValidator<MdSetup> metadataValidator,
        ISourcesRegistry sourcesRegistry,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
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
        _metadataValidator = metadataValidator;
        _sourcesRegistry = sourcesRegistry;
        _fileSystem = fileSystem;
        _cancellationToken = cancellationToken;
    }

    public Unit Build(IEnumerable<SyntaxUpdate> updates)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var logFile = _globalOptions.LogFile;
        using var logObserverToken= _observersRegistry.Register(_logObserver);
        try
        {
            using var logToken = _logger.TraceProcess("generation");
            ImmutableArray<MdSetup> setups;
            using (_logger.TraceProcess("metadata analysis"))
            {
                setups = _metadataBuilder.Build(updates).ToImmutableArray();
            }

            foreach (var setup in setups)
            {
                try
                {
                    _logger.Trace(setup.Hints, i => Enumerable.Repeat($"Settings \"{setup.Name.FullName}\":", 1).Concat(i.Select(j => $"{j.Key} = {j.Value}")));
                    using var setupToken = _logger.TraceProcess($"metadata processing \"{setup.Name.FullName}\", {setup.Bindings.Length} bindings");
                    _metadataValidator.Validate(setup);
                    DependencyGraph dependencyGraph;
                    using (_logger.TraceProcess($"building a dependency graph \"{setup.Name.FullName}\""))
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        dependencyGraph = _dependencyGraphBuilder.Build(setup);
                        foreach (var graphObserver in _observersProvider.GetObservers<DependencyGraph>())
                        {
                            graphObserver.OnNext(dependencyGraph);
                        }
                    }

                    using (_logger.TraceProcess($"search for roots \"{setup.Name.FullName}\""))
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        var roots = _rootsBuilder.Build(dependencyGraph);
                        dependencyGraph = dependencyGraph with { Roots = roots };
                    }

                    using (_logger.TraceProcess($"dependency graph validation \"{setup.Name.FullName}\""))
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        _dependencyGraphValidator.Validate(dependencyGraph);
                    }

                    CompositionCode composition;
                    using (_logger.TraceProcess($"creating composition \"{setup.Name.FullName}\""))
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        composition = _compositionBuilder.Build(dependencyGraph);
                    }

                    using (_logger.TraceProcess($"code generation \"{setup.Name.FullName}\""))
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        composition = _classBuilder.Build(composition);
                    }

                    using (_logger.TraceProcess($"saving data \"{setup.Name.FullName}\""))
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        var classCode = string.Join(Environment.NewLine, composition.Code);
                        _sourcesRegistry.AddSource($"{setup.Name.FullName}.g.cs", SourceText.From(classCode, Encoding.UTF8));
                    }

                    if (LoggerExtensions.IsTracing())
                    {
                        var trace = new List<string> { $"Setup {dependencyGraph.Source.Name.FullName}:", "---------- Map ----------" };
                        foreach (var item in dependencyGraph.Map)
                        {
                            trace.Add($"{item.Key} -> {item.Value}");
                        }

                        trace.Add("---------- Roots ----------");
                        foreach (var root in dependencyGraph.Roots)
                        {
                            trace.Add($"{root.Key} {root.Value.PropertyName} -> {root.Value.Node}");
                        }

                        _logger.Trace(trace, state => state);
                    }
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
                    ImmutableArray.Create("An unhandled error has occurred."),
                    default,
                    LogId.ErrorUnhandled,
                    error));
        }
        finally
        {
            stopwatch.Stop();
            _logObserver.Flush();
            try
            {
                if (logFile != null && !string.IsNullOrWhiteSpace(logFile))
                {
                    if (_fileSystem.GetDirectoryName(logFile) is {} logFileDirectory)
                    {
                        _fileSystem.CreateDirectory(logFileDirectory);
                    }

                    var log = _logObserver.Log;
                    log.Append(_logObserver.Outcome);
                    log.AppendLine($"{stopwatch.Elapsed.TotalMilliseconds,8:#####0.0} ms");
                    _fileSystem.AppendAllText(logFile, log.ToString());
                }
            }
            catch (Exception)
            {
                // ignored
            }
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
                ImmutableArray.Create("Generation interrupted."),
                default,
                LogId.InfoGenerationInterrupted,
                handledException));
    }
}