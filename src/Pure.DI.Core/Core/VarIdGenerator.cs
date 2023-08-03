// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class VarIdGenerator : IVarIdGenerator
{
    private int _id;

    public int NextId => _id++;
}