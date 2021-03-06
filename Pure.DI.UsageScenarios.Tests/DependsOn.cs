// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    // $visible=true
    // $tag=1 Basics
    // $priority=10
    // $description=Depends On
    // {
    static partial class MyBaseComposer
    {
        static MyBaseComposer() => DI.Setup()
            .Bind<IDependency>().To<Dependency>();
    }

    static partial class MyDependentComposer
    {
        static MyDependentComposer() => DI.Setup()
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
