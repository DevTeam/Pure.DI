using Pure.DI;
using static Pure.DI.Lifetime;

namespace _PureDIProjectName_;

internal partial class $(CompositionName)
{
    [global::System.Diagnostics.Conditional("DI")]
    private static void Setup() => 
        DI.Setup(nameof($(CompositionName)), CompositionKind.Internal)
            .Bind<IInput>().Bind<IOutput>().As(Singleton).To<ConsoleAdapter>();
}