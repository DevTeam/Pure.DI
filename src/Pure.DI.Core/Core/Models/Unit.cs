// ReSharper disable HeapView.ObjectAllocation.Evident
namespace Pure.DI.Core.Models;

internal readonly struct Unit
{
    public static readonly Unit Shared = new();

    public override string ToString() => string.Empty;
}