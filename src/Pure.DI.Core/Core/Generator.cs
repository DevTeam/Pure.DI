// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class Generator(
    IGlobalOptions globalOptions,
    IProfiler profiler,
    IExceptionHandler exceptionHandler,
    IObserversRegistry observersRegistry,
    IObserver<LogEntry> logObserver,
    IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> metadataBuilder,
    Func<IBuilder<MdSetup, Unit>> codeBuilderFactory,
    CancellationToken cancellationToken)
    : IBuilder<IEnumerable<SyntaxUpdate>, Unit>
{
    public Unit Build(IEnumerable<SyntaxUpdate> updates)
    {
        if (globalOptions.TryGetProfilePath(out var profilePath))
        {
            profiler.Profiling(profilePath);
        }
        
        using var logObserverToken= observersRegistry.Register(logObserver);
        try
        {
            exceptionHandler.SafeRun(updates, ProcessUpdates);
        }
        finally
        {
            logObserver.OnCompleted();
        }

        return Unit.Shared;
    }
    
    private void ProcessUpdates(IEnumerable<SyntaxUpdate> updates)
    {
        foreach (var setup in metadataBuilder.Build(updates))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            exceptionHandler.SafeRun(setup, BuildCode);
        }
    }
    
    private void BuildCode(MdSetup setup) =>
        codeBuilderFactory().Build(setup);
}