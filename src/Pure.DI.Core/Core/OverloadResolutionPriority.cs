// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

sealed class OverloadResolutionPriority(ITypes types) : IOverloadResolutionPriority
{
    public int Get(Compilation compilation, IMethodSymbol method)
    {
        var attributeType = types.TryGet(SpecialType.OverloadResolutionPriorityAttribute, compilation);
        if (attributeType is null)
        {
            return 0;
        }

        foreach (var attribute in method.GetAttributes())
        {
            if (!types.TypeEquals(attribute.AttributeClass, attributeType)
                || attribute.ConstructorArguments is not [{ Value: int priority }])
            {
                continue;
            }

            return priority;
        }

        return 0;
    }
}
