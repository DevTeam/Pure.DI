namespace Pure.DI.Core;

internal sealed class Generator : IBuilder<IEnumerable<SyntaxUpdate>, Unit>
{
    private readonly ILogger<Generator> _logger;
    private readonly IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> _metadataBuilder;
    private readonly Func<IBuilder<MdSetup, Unit>> _codeBuilderFactory;
    private readonly IObserversRegistry _observersRegistry;
    private readonly IObserver<LogEntry> _logObserver;

    public Generator(
        ILogger<Generator> logger,
        IObserversRegistry observersRegistry,
        IObserver<LogEntry> logObserver,
        IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> metadataBuilder,
        Func<IBuilder<MdSetup, Unit>> codeBuilderFactory)
    {
        _logger = logger;
        _observersRegistry = observersRegistry;
        _logObserver = logObserver;
        _metadataBuilder = metadataBuilder;
        _codeBuilderFactory = codeBuilderFactory;
    }

    public Unit Build(IEnumerable<SyntaxUpdate> updates)
    {
        using var logObserverToken= _observersRegistry.Register(_logObserver);
        try
        {
            foreach (var setup in _metadataBuilder.Build(updates))
            {
                try
                {
                    _codeBuilderFactory().Build(setup);
                }
                catch (CompileErrorException compileException)
                {
                    OnCompileException(compileException);
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
        catch (CompileErrorException compileException)
        {
            OnCompileException(compileException);
        }
        catch (HandledException handledException)
        {
            OnHandledException(handledException);
        }
        catch (Exception exception)
        {
            OnException(exception);
        }
        finally
        {
            _logObserver.OnCompleted();
        }
        
        return Unit.Shared;
    }

    private void OnCompileException(CompileErrorException exception) => 
        _logger.CompileError(exception.ErrorMessage, exception.Location, exception.Id);

    private void OnHandledException(Exception handledException) =>
        _logger.Log(
            new LogEntry(
#if DEBUG
                DiagnosticSeverity.Info,
#else
                DiagnosticSeverity.Hidden,
#endif
                "Code generation aborted.",
                default,
                LogId.InfoGenerationInterrupted,
                handledException));

    private void OnException(Exception exception) =>
        _logger.Log(
            new LogEntry(
                DiagnosticSeverity.Error,
                "An unhandled error has occurred.",
                default,
                LogId.ErrorUnhandled,
                exception));
}