// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using System.Globalization;

sealed class CodeGenerator(
    IProfiler profiler,
    IExceptionHandler exceptionHandler,
    IObserversRegistry observersRegistry,
    IObserver<LogEntry> logObserver,
    IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>> metadataBuilder,
    Func<IBuilder<MdSetup, CompositionCode?>> codeBuilderFactory,
    IBuilder<TagContext, IEnumerable<TagCode>> tagClassBuildr,
    ISources sources,
    IGlobalProperties globalProperties,
    CancellationToken cancellationToken)
    : IBuilder<IEnumerable<SyntaxUpdate>, Unit>
{
    public Unit Build(IEnumerable<SyntaxUpdate> updates)
    {
        profiler.Profile();
        var culture = globalProperties.Culture;
        CultureInfo? currentCulture = null;
        if (culture is not null)
        {
            currentCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = culture;
        }

        try
        {
            using var logObserverToken = observersRegistry.Register(logObserver);
            try
            {
                exceptionHandler.SafeRun(updates, ProcessUpdates);
            }
            finally
            {
                logObserver.OnCompleted();
            }
        }
        finally
        {
            if (currentCulture is not null)
            {
                CultureInfo.CurrentUICulture = currentCulture;
            }
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

    private Unit BuildSmartTags(TagContext tagContext)
    {
        foreach (var tagCode in tagClassBuildr.Build(tagContext))
        {
            using var rent = tagCode.Code.SaveToArray(Encoding.UTF8, out var buffer, out var size);
            sources.AddSource($"{Names.GeneratorName}.Components.Api.{tagCode.ClassName}{Names.CodeFileSuffix}", SourceText.From(buffer, size, Encoding.UTF8, SourceHashAlgorithm.Sha1, false, true));
        }

        return Unit.Shared;
    }

    private CompositionCode? BuildCode(MdSetup setup) =>
        codeBuilderFactory().Build(setup);
}