using Pure.DI;
using static Pure.DI.Lifetime;

namespace _PureDIProjectName_;

internal partial class $(CompositionName)
{
    private static void Setup() => 
        DI.Setup(nameof($(CompositionName)))
        .Bind<IInput>().Bind<IOutput>().As(Singleton).To<ConsoleAdapter>()
        .Root<Program>("Root");
}