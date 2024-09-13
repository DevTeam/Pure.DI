// ReSharper disable UnusedMember.Local

namespace Pure.DI.Integration;

using Pure.DI;

public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exposed);
}