// ReSharper disable HeapView.ObjectAllocation.Evident

namespace Pure.DI.Core;

sealed class HandledException : Exception
{
    public static readonly HandledException Shared = new();

    private HandledException()
    {
    }
}