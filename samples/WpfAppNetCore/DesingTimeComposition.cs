namespace WpfAppNetCore;

using Pure.DI;

internal partial class DesignTimeComposition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<IAppViewModel>(nameof(App))
        .Root<IClockViewModel>(nameof(Clock))

        .Bind().To<DesignTimeAppViewModel>()
        .Bind().To<DesignTimeClockViewModel>();
}