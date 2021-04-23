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
                .Bind<IClockViewModel>().To<ClockViewModel>()

                // Models
                .Bind<ITimer>().As(Scoped).To(_ => new Timer(TimeSpan.FromSeconds(1)))
                .Bind<IClock>().As(ContainerSingleton).To<SystemClock>();
        }
    }
}