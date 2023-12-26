// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class CodeBuilder(
    IObserversProvider observersProvider,
    IBuilder<MdSetup, DependencyGraph> dependencyGraphBuilder,
    IValidator<DependencyGraph> dependencyGraphValidator,
    IBuilder<DependencyGraph, IReadOnlyDictionary<Injection, Root>> rootsBuilder,
    [Tag(WellknownTag.CompositionBuilder)] IBuilder<DependencyGraph, CompositionCode> compositionBuilder,
    [Tag(WellknownTag.ClassBuilder)] IBuilder<CompositionCode, CompositionCode> classBuilder,
    IValidator<MdSetup> metadataValidator,
    IGeneratorSources sources,
    CancellationToken cancellationToken)
    : IBuilder<MdSetup, Unit>
{
    public Unit Build(MdSetup setup)
    {
        if (!metadataValidator.Validate(setup))
        {
            return Unit.Shared;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var dependencyGraph = dependencyGraphBuilder.Build(setup);
        foreach (var graphObserver in observersProvider.GetObservers<DependencyGraph>())
        {
            graphObserver.OnNext(dependencyGraph);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var roots = rootsBuilder.Build(dependencyGraph);
        dependencyGraph = dependencyGraph with { Roots = roots };

        cancellationToken.ThrowIfCancellationRequested();
        if (!dependencyGraphValidator.Validate(dependencyGraph))
        {
            return Unit.Shared;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var composition = compositionBuilder.Build(dependencyGraph);

        cancellationToken.ThrowIfCancellationRequested();
        composition = classBuilder.Build(composition);

        cancellationToken.ThrowIfCancellationRequested();
        var classCode = string.Join(Environment.NewLine, composition.Code);
        sources.AddSource($"{setup.Name.FullName}.g.cs", SourceText.From(classCode, Encoding.UTF8));
        return Unit.Shared;
    }
}