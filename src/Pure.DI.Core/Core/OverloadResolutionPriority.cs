// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

sealed class OverloadResolutionPriority(ITypes types) : IOverloadResolutionPriority
{
    public int Get(Compilation compilation, IMethodSymbol method) =>
        TryGet(compilation, method, out var priority) ? priority : 0;

    public bool TryGet(Compilation compilation, IMethodSymbol method, out int priority)
    {
        var attributeType = types.TryGet(SpecialType.OverloadResolutionPriorityAttribute, compilation);
        if (attributeType is null)
        {
            priority = 0;
            return false;
        }

        foreach (var attribute in method.GetAttributes())
        {
            if (!types.TypeEquals(attribute.AttributeClass, attributeType)
                || attribute.ConstructorArguments is not [{ Value: int value }])
            {
                continue;
            }

            priority = value;
            return true;
        }

        priority = 0;
        return false;
    }
}
