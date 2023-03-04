// ReSharper disable HeapView.ObjectAllocation.Evident
namespace Pure.DI.Core;

internal class HandledException: Exception
{
    public static readonly HandledException Shared = new();

    private HandledException() { }
}