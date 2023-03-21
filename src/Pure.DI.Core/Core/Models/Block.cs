namespace Pure.DI.Core.Models;

internal record Block(Variable Root, ImmutableArray<Instantiation> Instantiations)
{
    public override string ToString() => $"{Root}{Environment.NewLine}{{{Environment.NewLine}{string.Join(Environment.NewLine, Instantiations.Select(i => $"  {i}"))}{Environment.NewLine}}}";
}