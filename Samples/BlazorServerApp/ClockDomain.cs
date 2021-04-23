// ReSharper disable UnusedType.Global
namespace BlazorServerApp
{
    using System;
    using Clock.Models;
    using Clock.ViewModels;
    using Pure.DI;
    using static Pure.DI.Lifetime;

    internal static partial class ClockDomain
    {
        static ClockDomain()
        {
            DI.Setup()
                // Infrastructure
                .Bind<IDispatcher>().As(Singleton).To<Dispatcher>()

                // View Models
                .Bind<IClockViewModel>().As(Scoped).To<ClockViewModel>()

                // Models
                .Bind<ITimer>().As(ContainerSingleton).To(_ => new Timer(TimeSpan.FromSeconds(1)))
                .Bind<IClock>().As(Singleton).To<SystemClock>();
        }
    }
}