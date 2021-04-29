// ReSharper disable UnusedType.Global
// ReSharper disable PartialTypeWithSinglePart
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
            // output=C:\Projects\DevTeam\Pure.DI\out
            // verbosity=normal
            // #debug=true
            DI.Setup()

                // View Models
                .Bind<IClockViewModel>().To<ClockViewModel>()

                // Models
                .Bind<ILog<TT>>().As(Singleton).To<AspNetLog<TT>>()
                .Bind<ITimer>().As(Scoped).To(_ => new Timer(TimeSpan.FromSeconds(1)))
                .Bind<IClock>().As(ContainerSingleton).To<SystemClock>();
        }
    }
}