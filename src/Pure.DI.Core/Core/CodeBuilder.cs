// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class CodeBuilder(
    IObserversProvider observersProvider,
    IBuilder<MdSetup, DependencyGraph> dependencyGraphBuilder,
    IValidator<DependencyGraph> dependencyGraphValidator,
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
        if (!dependencyGraphValidator.Validate(dependencyGraph))
        {
            return Unit.Shared;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var composition = compositionBuilder.Build(dependencyGraph);

        cancellationToken.ThrowIfCancellationRequested();
        composition = classBuilder.Build(composition);

        cancellationToken.ThrowIfCancellationRequested();
        var code = new StringBuilder(composition.Code.Sum(i => i.Length + 2));
        foreach (var line in composition.Code)
        {
            code.AppendLine(line);
        }
        
        cancellationToken.ThrowIfCancellationRequested();
        sources.AddSource($"{setup.Name.FullName}.g.cs", SourceText.From(code.ToString(), Encoding.UTF8));
        return Unit.Shared;
    }
}