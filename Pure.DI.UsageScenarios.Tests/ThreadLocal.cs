// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Xunit;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ThreadLocal
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=01
            // $description=Thread Local
            // $footer=This sample references types from [this file](Pure.DI.UsageScenarios.Tests/Models.cs).
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()
                // Explicitly specifies to provide the composition root of type ThreadLocal<IService>
                .Root<ThreadLocal<IService>>();

            // Resolve the instance of ThreadLocal<IService>
            var threadLocal = ThreadLocalDI.Resolve<ThreadLocal<IService>>();

            // Get the instance via ThreadLocal
            var instance = threadLocal.Value;
            // }
            // Check the instance
            instance.ShouldBeOfType<Service>();
        }
    }
}
