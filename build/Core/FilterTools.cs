// ReSharper disable MemberCanBeMadeStatic.Global
namespace Build.Core;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
class FilterTools
{
    public bool AddExample(AIContextSize size, int priority, string groupName)
    {
        switch (size)
        {
            case AIContextSize.Small
                when priority <= 6 && groupName is "Basics" or "Lifetimes":

            case AIContextSize.Medium
                when
                groupName is "Basics"
                || priority < 3 && groupName is not "Advanced" and not "Unity" and not "Hints" and not "UseCases":

            case AIContextSize.Large:
                return true;

            default:
                return false;
        }
    }
}