using Pure.DI;
using static Pure.DI.Lifetime;

namespace WinFormsApp;

partial class Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<Program>(nameof(Root))

        .Bind().As(Singleton).To<FormMain>()
        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WinFormsDispatcher>();
}