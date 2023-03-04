// ReSharper disable HeapView.ObjectAllocation
namespace Pure.DI.Core.Models;

internal readonly record struct MdDefaultLifetime(
    in MdLifetime Lifetime)
{
    public override string ToString() => $"DefaultLifetime({Lifetime})";
}