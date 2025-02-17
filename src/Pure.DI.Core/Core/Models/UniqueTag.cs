// ReSharper disable NotAccessedPositionalProperty.Global

namespace Pure.DI.Core.Models;

record UniqueTag(int Id)
{
    public override string ToString() =>
        $"Unique tag {Id}";
}