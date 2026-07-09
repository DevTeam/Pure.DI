// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Global
namespace WpfAppNetCore;

using Pure.DI;
using static Pure.DI.RootKinds;

partial class DesignTimeComposition: Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        // Overrides virtual roots with design-time view models
        .Root<IAppViewModel>(nameof(App), kind: Override)
        .Root<IClockViewModel>(nameof(Clock), kind: Override)

        .Bind().To<DesignTimeAppViewModel>()
        .Bind().To<DesignTimeClockViewModel>();
}
