namespace Pure.DI.Core.Models;

internal record Block(Variable Root, ImmutableArray<Instantiation> Resolves)
{
    public override string ToString() => $"{Root}{Environment.NewLine}{{{Environment.NewLine}{string.Join(Environment.NewLine, Resolves.Select(i => $"  {i}"))}{Environment.NewLine}}}";
}