// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal class ExceptionHandler(ILogger logger)
    : IExceptionHandler
{
    public TResult? SafeRun<T, TResult>(T state, Func<T, TResult> action)
    {
        try
        {
            return action(state);
        }
        catch (AggregateException aggregateException)
        {
            OnAggregateException(aggregateException);
        }
        catch (CompileErrorException compileException)
        {
            OnCompileException(compileException);
        }
        catch (HandledException handledException)
        {
            OnHandledException(handledException);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            OnException(exception);
        }

        return default;
    }

    private void OnAggregateException(AggregateException aggregateException)
    {
        foreach (var exception in aggregateException.InnerExceptions)
        {
            switch (exception)
            {
                case CompileErrorException compileError:
                    OnCompileException(compileError);
                    break;

                case HandledException handledException:
                    OnHandledException(handledException);
                    break;

                default:
                    OnException(exception);
                    break;
            }
        }
    }

    private void OnCompileException(CompileErrorException exception) =>
        logger.CompileError(exception.ErrorMessage, exception.Location, exception.Id);

    private void OnHandledException(HandledException handledException) =>
        logger.Log(
            new LogEntry(
#if DEBUG
                DiagnosticSeverity.Info,
#else
                DiagnosticSeverity.Hidden,
#endif
                "Code generation aborted.",
                null,
                LogId.InfoGenerationInterrupted,
                handledException));

    private void OnException(Exception exception) =>
        logger.Log(
            new LogEntry(
                DiagnosticSeverity.Error,
                "An unhandled error has occurred.",
                null,
                LogId.ErrorUnhandled,
                exception));
}