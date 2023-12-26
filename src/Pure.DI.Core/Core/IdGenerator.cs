// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class IdGenerator : IIdGenerator
{
    private int _id;
    
    public int Generate() => Interlocked.Increment(ref _id);
}