using Pure.DI;
using static Pure.DI.Lifetime;
using static Pure.DI.RootKinds;
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedMember.Local

namespace AvaloniaApp;

partial class Composition
{
    void Setup() => DI.Setup()
        // Single composition root for the application
        .Root<Root>(nameof(Root), kind: Virtual)
        .Bind().As(Singleton).To<Root>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<AvaloniaDispatcher>();
}