// ReSharper disable UnusedMember.Local
namespace AvaloniaSimpleApp;

using System.Diagnostics;
using Pure.DI;
using static Pure.DI.RootKinds;

public partial class DesignTimeComposition: Composition
{
    [Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        // Overrides virtual roots with design-time view models
        .Root<IMainViewModel>(nameof(MainViewModel), kind: Override)

        .Bind().To<DesignTimeMainViewModel>();
}
