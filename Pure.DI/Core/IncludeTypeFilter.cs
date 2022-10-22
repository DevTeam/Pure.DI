// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Text.RegularExpressions;
using NS35EBD81B;

internal sealed class IncludeTypeFilter : IIncludeTypeFilter
{
    public bool IsAccepted(SemanticType factoryType, Dependency dependency) =>
        Match<IncludeAttribute>(factoryType, dependency, true)
        && !Match<ExcludeAttribute>(factoryType, dependency, false);

    private static bool Match<TAttribute>(SemanticType factoryType, Dependency dependency, bool defaultValue)
        where TAttribute : Attribute => (
                from attrData in factoryType.Type.GetAttributes(typeof(TAttribute), factoryType.SemanticModel)
                let args = attrData.ConstructorArguments
                where args.Length == 1
                select Match(args.Single(), dependency))
            .DefaultIfEmpty(defaultValue)
            .Any(i => i);

    private static bool Match(TypedConstant typedConstant, Dependency dependency)
    {
        var argType = typedConstant.Type;
        if (argType == default)
        {
            return false;
        }
        
        switch (typedConstant.Value)
        {
            case string expression when string.IsNullOrWhiteSpace(expression):
                return false;
          
            case string expression:
            {
                var regex = new Regex(expression);
                return regex.IsMatch(dependency.Implementation.Name);
            }
            
            case int value:
                return (int)dependency.Binding.Lifetime == value;
            
            default:
                return false;
        }
    }
}