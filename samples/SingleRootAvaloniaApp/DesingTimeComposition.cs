namespace AvaloniaApp;

using Pure.DI;
using static Pure.DI.RootKinds;

partial class DesignTimeComposition: Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")
        // Single composition root for the application
        .Root<Root>(nameof(Root), kind: Override)

        .Bind().To<DesignTimeAppViewModel>()
        .Bind().To<DesignTimeClockViewModel>();
}