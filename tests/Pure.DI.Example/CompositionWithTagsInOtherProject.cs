// ReSharper disable UnusedMember.Local
namespace Pure.DI.Integration;
using Pure.DI;

public partial class CompositionWithTagsInOtherProject
{
    private static void Setup() => 
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind("Some tag").To<MyService>()
            .Root<IMyService>("MyService", "Some tag", RootKinds.Exposed);
}