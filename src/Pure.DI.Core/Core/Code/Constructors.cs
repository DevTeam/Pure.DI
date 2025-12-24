namespace Pure.DI.Core.Code;

class Constructors : IConstructors
{
    public bool IsEnabled(CompositionCode composition, ConstructorKind kind) =>
        kind switch
        {
            ConstructorKind.Default => composition.ClassArgs.Length == 0,
            ConstructorKind.Parameterized => composition.ClassArgs.Length > 0,
            ConstructorKind.Scope => true,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
}