// ReSharper disable MemberCanBeMadeStatic.Global
namespace Build.Core;

using Doc;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
class FilterTools
{
    public bool AddExample(AIContextSize size, int priority, string groupName)
    {
        switch (size)
        {
            case AIContextSize.Small
                when priority < 4 && groupName is "Basics" or "Lifetimes":
            case AIContextSize.Medium
                when priority <= 8 && groupName is not "Advanced" and not "Hints" and not "UseCases":
            case AIContextSize.Large:
                return true;

            default:
                return false;
        }
    }

    public bool DocumentPartFilter(AIContextSize size, DocumentPart part)
    {
        if (part.NamespaceName != "Pure.DI" || part.TypeName == "Generator")
        {
            return false;
        }

        return size switch
        {
            AIContextSize.Small => false,
            AIContextSize.Medium => false,
            _ =>
                part.TypeName is not "Buckets`2" and not "IResolver`2" and not "Pair`2" and not "Strings"
                && !Enumerable.Range(5, 10).Any(i => part.MemberName.Contains(i.ToString()))
        };
    }
}