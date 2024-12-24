// ReSharper disable UnusedMember.Local

namespace OtherAssembly;

using Pure.DI;

public partial class CompositionWithGenericRootsInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Hint(Hint.Resolve, "Off")
            .Bind().To(_ => 99)
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyGenericService<TT>>()
            .Root<IMyGenericService<TT>>("GetMyService", kind: RootKinds.Exposed);
}