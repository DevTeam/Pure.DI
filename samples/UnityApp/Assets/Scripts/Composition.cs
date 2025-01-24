using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    public static readonly Composition Shared = new();
    
    private void Setup() =>
        DI.Setup()
            .Bind().As(Singleton).To<ClockService>()
            .Bind().To<ClockViewModel>()
            .Builder<Clock>("BuildUp");
}
