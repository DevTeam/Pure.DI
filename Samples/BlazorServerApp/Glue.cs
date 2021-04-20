namespace BlazorServerApp
{
    using System;
    using Clock.Models;
    using Clock.ViewModels;
    using Pure.DI;
    using static Pure.DI.Lifetime;

    internal static partial class Glue
    {
        static Glue()
        {
            DI.Setup()
                // Infrastructure
                .Bind<IDispatcher>().As(Singleton).To<Dispatcher>()

                // View Models
                .Bind<IClockViewModel>().To<ClockViewModel>()

                // Models
                .Bind<ITimer>().As(Singleton).To(_ => new Timer(TimeSpan.FromSeconds(1)))
                .Bind<IClock>().As(Singleton).To<SimpleClock>();
        }
    }
}