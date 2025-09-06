// ReSharper disable HeapView.ObjectAllocation.Evident

namespace Pure.DI.Core;

[Serializable]
sealed class HandledException : Exception
{
    public static readonly HandledException Shared = new();

    private HandledException() { }
}