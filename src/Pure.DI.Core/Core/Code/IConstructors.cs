namespace Pure.DI.Core.Code;

interface IConstructors
{
    bool IsEnabled(DependencyGraph graph);

    bool IsEnabled(CompositionCode composition, ConstructorKind kind);
}