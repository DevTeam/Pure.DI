using System.Diagnostics;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace _PureDIProjectName_;

/// <summary>
/// Pure.DI Composition Setup. Please see <see href="https://github.com/DevTeam/Pure.DI.Solution">this</see> example.
/// </summary>
internal class $(CompositionName)
{
    [Conditional("DI")]
    void Setup() => 
        DI.Setup(nameof($(CompositionName)), CompositionKind.Global)
            .Bind().As(Singleton).To<ConsoleAdapter>();
}