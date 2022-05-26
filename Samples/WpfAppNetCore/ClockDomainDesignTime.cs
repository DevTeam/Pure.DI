// ReSharper disable UnusedType.Global
// ReSharper disable RedundantUsingDirective
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ArrangeNamespaceBody
namespace WpfAppNetCore
{
    using System;
    using Clock.Models;
    using Clock.ViewModels;
    using Pure.DI;
    using Views;
    using static Pure.DI.Lifetime;

    internal static partial class ClockDomainDesignTime
    {
        static ClockDomainDesignTime() => DI.Setup().DependsOn(nameof(ClockDomain))
            // Design-time View Models
            .Bind<IClockViewModel>().As(Singleton).To<ClockViewModelDesignTime>();
    }
}