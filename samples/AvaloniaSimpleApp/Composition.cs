// ReSharper disable UnusedMember.Local
namespace AvaloniaSimpleApp;

using System.Diagnostics;
using Pure.DI;
using static Pure.DI.Lifetime;
using static Pure.DI.RootKinds;

public partial class Composition
{
    [Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Root<MainWindow>(nameof(MainWindow))
        .Root<IMainViewModel>(nameof(MainViewModel), kind: Virtual)
        .Bind().As(Singleton).To<Repository>()
        .Bind().To<BusinessService>()
        .Bind().As(Singleton).To<MainViewModel>();
}
