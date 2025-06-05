// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using static Tag;

sealed class CodeBuilder(
    IObserversProvider observersProvider,
    IBuilder<MdSetup, DependencyGraph?> dependencyGraphBuilder,
    IEnumerable<IValidator<MdSetup>> metadataValidators,
    IEnumerable<IValidator<DependencyGraph>> dependencyGraphValidators,
    IEnumerable<IValidator<CompositionCode>> compositionValidators,
    IBuilder<DependencyGraph, DependencyGraph> rootsBuilder,
 #pragma warning disable CS9113 // Parameter is unread.
    IBuilder<DependencyGraph, CompositionCode> compositionBuilder,
 #pragma warning restore CS9113 // Parameter is unread.
    [Tag(2)] IBuilder<DependencyGraph, CompositionCode> v2CompositionBuilder,
    [Tag(CompositionClass)] IBuilder<CompositionCode, CompositionCode> compositionClassBuilder,
    ISources sources,
    CancellationToken cancellationToken)
    : IBuilder<MdSetup, CompositionCode?>
{
    public CompositionCode? Build(MdSetup setup)
    {
        if (metadataValidators.Any(validator => !validator.Validate(setup)))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var dependencyGraph = dependencyGraphBuilder.Build(setup);
        if (dependencyGraph is null)
        {
            return null;
        }

        foreach (var graphObserver in observersProvider.GetObservers<DependencyGraph>())
        {
            graphObserver.OnNext(dependencyGraph);
        }

        cancellationToken.ThrowIfCancellationRequested();
        dependencyGraph = rootsBuilder.Build(dependencyGraph);

        cancellationToken.ThrowIfCancellationRequested();
        if (dependencyGraphValidators.Any(validator => !validator.Validate(dependencyGraph)))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        CompositionCode? composition = null;
        Exception? error = null;
        var thread = new Thread(() => {
            try
            {
                composition = v2CompositionBuilder.Build(dependencyGraph);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        }, 1024 * 1024 * 10) { IsBackground = true, Priority = ThreadPriority.Highest, Name = "Composition Builder" };
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            throw error;
        }

        if (composition is null)
        {
            throw HandledException.Shared;
        }

        if (compositionValidators.Any(validator => !validator.Validate(composition)))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        composition = compositionClassBuilder.Build(composition);

        cancellationToken.ThrowIfCancellationRequested();
        using var rent = composition.Code.SaveToArray(Encoding.UTF8, out var buffer, out var size);
        sources.AddSource($"{setup.Name.FullName}{Names.CodeFileSuffix}", SourceText.From(buffer, size, Encoding.UTF8, SourceHashAlgorithm.Sha1, false, true));

        return composition;
    }
}