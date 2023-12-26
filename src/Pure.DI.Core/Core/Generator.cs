// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class Generator(
    ILogger<Generator> logger,
    IObserversRegistry observersRegistry,
    IObserver<LogEntry> logObserver,
    IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> metadataBuilder,
    Func<IBuilder<MdSetup, Unit>> codeBuilderFactory)
    : IBuilder<IEnumerable<SyntaxUpdate>, Unit>
{
    public Unit Build(IEnumerable<SyntaxUpdate> updates)
    {
        using var logObserverToken= observersRegistry.Register(logObserver);
        try
        {
            foreach (var setup in metadataBuilder.Build(updates))
            {
                try
                {
                    codeBuilderFactory().Build(setup);
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
            logObserver.OnCompleted();
        }
        
        return Unit.Shared;
    }

    private void OnCompileException(CompileErrorException exception) => 
        logger.CompileError(exception.ErrorMessage, exception.Location, exception.Id);

    private void OnHandledException(Exception handledException) =>
        logger.Log(
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
        logger.Log(
            new LogEntry(
                DiagnosticSeverity.Error,
                "An unhandled error has occurred.",
                default,
                LogId.ErrorUnhandled,
                exception));
}