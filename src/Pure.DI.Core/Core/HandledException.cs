// ReSharper disable HeapView.ObjectAllocation.Evident
namespace Pure.DI.Core;

internal sealed class HandledException: Exception
{
    public static readonly HandledException Shared = new();

    private HandledException() { }
}