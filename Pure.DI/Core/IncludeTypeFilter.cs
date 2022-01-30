// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Text.RegularExpressions;

internal class IncludeTypeFilter : IIncludeTypeFilter
{
    public bool IsAccepted(SemanticType factoryType, SemanticType targetType) =>
        Match<IncludeAttribute>(factoryType, targetType, true)
        && !Match<ExcludeAttribute>(factoryType, targetType, false);

    private static bool Match<TAttribute>(SemanticType factoryType, SemanticType targetType, bool defaultValue)
        where TAttribute : Attribute
        => (
                from attrData in factoryType.Type.GetAttributes(typeof(TAttribute), factoryType.SemanticModel)
                let args = attrData.ConstructorArguments
                where args.Length == 1
                let expression = args.Single().Value as string
                where !string.IsNullOrWhiteSpace(expression)
                let regex = new Regex(expression)
                select regex.IsMatch(targetType.Name))
            .DefaultIfEmpty(defaultValue)
            .Any(i => i);
}