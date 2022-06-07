// ReSharper disable UnusedType.Global
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ArrangeNamespaceBody
namespace Clock
{
    using Models;
    using ViewModels;
    using static Lifetime;

    public static partial class DefaultClockDomain
    {
        static DefaultClockDomain() =>
            DI.Setup()
                // Set default lifetime
                .Default(Singleton)
                
                // View Models
                .Bind<IClockViewModel>().As(Transient).To<ClockViewModel>()

                // Models
                .Bind<ILog<TT>>().To<Log<TT>>()
                .Bind<ITimer>().To(_ => new Timer(TimeSpan.FromSeconds(1)))
                .Bind<IClock>().To<SystemClock>();
    }
}