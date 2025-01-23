using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    public static readonly Composition Shared = new();
    
    private void Setup() =>
        DI.Setup()
            .Hint(Hint.Resolve, "Off")
            .Bind().As(Singleton).To<ClockService>()
            .Bind().To<ClockViewModel>()
            .RootArg<Clock>("clock", "arg")
            .Bind().To(ctx =>
            {
                ctx.Inject("arg", out Clock clock);
                ctx.BuildUp(clock);
                return clock;
            })
            .Root<Clock>("BuildUp");
}
