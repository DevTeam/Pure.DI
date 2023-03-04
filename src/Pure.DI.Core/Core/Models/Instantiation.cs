namespace Pure.DI.Core.Models;

internal record Instantiation(Variable Target, ImmutableArray<Variable> Arguments)
{
    public override string ToString() => $"{Target}({string.Join(", ", Arguments)})";
}