// ReSharper disable UnusedMember.Local
namespace AvaloniaSimpleApp;

using Pure.DI;
using static Pure.DI.Lifetime;

public partial class Composition
{
    private static void Setup() => DI.Setup()
        .Root<MainWindow>(nameof(MainWindow))
        .Root<IMainViewModel>(nameof(MainViewModel))
        .Bind().As(Singleton).To<Repository>()
        .Bind().To<BusinessService>()
        .Bind().As(Singleton).To<MainViewModel>();
}