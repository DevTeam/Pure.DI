// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class CodeGenerator(
    IProfiler profiler,
    IExceptionHandler exceptionHandler,
    IObserversRegistry observersRegistry,
    IObserver<LogEntry> logObserver,
    IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> metadataBuilder,
    Func<IBuilder<MdSetup, CompositionCode?>> codeBuilderFactory,
    IBuilder<TagContext, TagCode> tagClassBuildr,
    ISmartTags smartTags,
    ISources sources,
    CancellationToken cancellationToken)
    : IBuilder<IEnumerable<SyntaxUpdate>, Unit>
{
    public Unit Build(IEnumerable<SyntaxUpdate> updates)
    {
        profiler.Profile();
        using var logObserverToken = observersRegistry.Register(logObserver);
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

    private Unit ProcessUpdates(IEnumerable<SyntaxUpdate> updates)
    {
        var compositions = new List<CompositionCode>();
        foreach (var setup in metadataBuilder.Build(updates))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (exceptionHandler.SafeRun(setup, BuildCode) is {} composition)
            {
                compositions.Add(composition);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        exceptionHandler.SafeRun(new TagContext(compositions), BuildSmartTags);
        return Unit.Shared;
    }

    private TagCode? BuildSmartTags(TagContext tagContext)
    {
        var tags = smartTags.GetAll();
        if (tags.Count <= 0)
        {
            return null;
        }

        var tagCode = tagClassBuildr.Build(tagContext);
        using var rent = tagCode.Code.SaveToArray(Encoding.UTF8, out var buffer, out var size);
        sources.AddSource($"{Names.GeneratorName}.Components.Api.{nameof(Tag)}.g.cs", SourceText.From(buffer, size, Encoding.UTF8, SourceHashAlgorithm.Sha1, false, true));
        return tagCode;
    }

    private CompositionCode? BuildCode(MdSetup setup) =>
        codeBuilderFactory().Build(setup);
}