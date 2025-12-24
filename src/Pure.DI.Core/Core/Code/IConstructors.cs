namespace Pure.DI.Core.Code;

interface IConstructors
{
    bool IsEnabled(CompositionCode composition, ConstructorKind kind);
}