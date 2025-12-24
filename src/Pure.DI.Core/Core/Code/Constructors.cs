namespace Pure.DI.Core.Code;

class Constructors(
    IRegistry<int> registry)
    : IConstructors
{
    public bool IsEnabled(CompositionCode composition, ConstructorKind kind) =>
        kind switch
        {
            ConstructorKind.Default => composition.ClassArgs.Length == 0,
            ConstructorKind.Parameterized => composition.ClassArgs.Length > 0,
            ConstructorKind.Scope => IsScopedConstructorEnabled(composition),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    private bool IsScopedConstructorEnabled(CompositionCode composition)
    {
        var scopeNodes =
            from node in composition.Source.Graph.Vertices
            where node.ActualLifetime == Lifetime.Scoped
            where registry.IsRegistered(composition.Source.Source, node.Binding.Id)
            select node;

       return scopeNodes.Any();
    }
}