// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedMember.Local
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    // $visible=true
    // $tag=1 Basics
    // $priority=10
    // $description=Depends On
    // $header=Sometimes it becomes necessary to reuse a set of bindings in several composers. To do this, you can use the `DependsOn` function passing the name of the composer, where to get the set for reuse. It is important to note that this method works for composers within the same project.
    // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
    // {
    static partial class MyBaseComposer
    {
        static void Setup() => DI.Setup()
            .Bind<IDependency>().To<Dependency>();
    }

    static partial class MyDependentComposer
    {
        static void Setup() => DI.Setup()
            .DependsOn(nameof(MyBaseComposer))
            .Bind<IService>().To<Service>();
    }

    // }

    public class DependsOn
    {
        [Fact]
        public void Run()
        {
    // {
    // Resolve an instance of interface `IService`
    var instance = MyDependentComposer.Resolve<IService>();
    // }
            instance.ShouldBeOfType<Service>();
        }
    }
}
