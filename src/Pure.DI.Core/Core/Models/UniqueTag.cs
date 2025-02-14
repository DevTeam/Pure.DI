// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

internal record UniqueTag(int Id)
{
    public override string ToString() =>
        $"Unique tag {Id}";
}