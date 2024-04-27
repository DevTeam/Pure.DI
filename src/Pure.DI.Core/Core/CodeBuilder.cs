// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class CodeBuilder(
    IObserversProvider observersProvider,
    IBuilder<MdSetup, DependencyGraph?> dependencyGraphBuilder,
    IEnumerable<IValidator<MdSetup>> metadataValidators,
    IEnumerable<IValidator<DependencyGraph>> dependencyGraphValidators,
    IEnumerable<IValidator<CompositionCode>> compositionValidators,
    IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> rootsBuilder,
    IBuilder<DependencyGraph, CompositionCode> compositionBuilder,
    IBuilder<CompositionCode, CompositionCode> classBuilder,
    IGeneratorSources sources,
    CancellationToken cancellationToken)
    : IBuilder<MdSetup, Unit>
{
    public Unit Build(MdSetup setup)
    {
        if (metadataValidators.Any(validator => !validator.Validate(setup)))
        {
            return Unit.Shared;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var dependencyGraph = dependencyGraphBuilder.Build(setup);
        if (dependencyGraph is null)
        {
            return Unit.Shared;
        }

        foreach (var graphObserver in observersProvider.GetObservers<DependencyGraph>())
        {
            graphObserver.OnNext(dependencyGraph);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var roots = rootsBuilder.Build(dependencyGraph);
        dependencyGraph = dependencyGraph with { Roots = roots };

        cancellationToken.ThrowIfCancellationRequested();
        if (dependencyGraphValidators.Any(validator => !validator.Validate(dependencyGraph)))
        {
            return Unit.Shared;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var composition = compositionBuilder.Build(dependencyGraph);

        if (compositionValidators.Any(validator => !validator.Validate(composition)))
        {
            return Unit.Shared;
        }

        cancellationToken.ThrowIfCancellationRequested();
        composition = classBuilder.Build(composition);

        cancellationToken.ThrowIfCancellationRequested();
        using var rent = composition.Code.SaveToArray(Encoding.UTF8, out var buffer, out var size);
        sources.AddSource($"{setup.Name.FullName}.g.cs", SourceText.From(buffer, size, Encoding.UTF8, SourceHashAlgorithm.Sha1, false, true));
        return Unit.Shared;
    }
}