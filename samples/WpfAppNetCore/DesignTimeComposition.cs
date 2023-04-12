// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantNameQualifier
namespace WpfAppNetCore;

using Clock.ViewModels;
using Pure.DI;

internal partial class DesignTimeComposition
{
    private static void SetupDesignTime() => DI.Setup(nameof(DesignTimeComposition))
        // Design-time View Models
        .Bind<IClockViewModel>().To<ClockViewModelDesignTime>().Root<IClockViewModel>();    
}