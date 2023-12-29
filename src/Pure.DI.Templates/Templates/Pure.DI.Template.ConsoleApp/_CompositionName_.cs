using Pure.DI;
using static Pure.DI.Lifetime;

namespace _PureDIProjectName_;

/// <summary>
/// Pure.DI Composition Setup. Please see <see href="https://github.com/DevTeam/Pure.DI.Solution">this</see> example.
/// </summary>
internal partial class $(CompositionName)
{
    [Conditional("DI")]
    private static void Setup() => 
        DI.Setup(nameof($(CompositionName)))
            .Bind<IInput, IOutput>().As(Singleton).To<ConsoleAdapter>()
            .Root<Program>("Root");
}