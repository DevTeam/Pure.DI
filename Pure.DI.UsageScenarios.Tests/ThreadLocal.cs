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
            // $description=ThreadLocal
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()
                .Bind<CompositionRoot<ThreadLocal<IService>>>().To<CompositionRoot<ThreadLocal<IService>>>();

            // Resolve the instance of ThreadLocal<IService>
            var threadLocal = ThreadLocalDI.Resolve<CompositionRoot<ThreadLocal<IService>>>().Root;

            // Get the instance via ThreadLocal
            var instance = threadLocal.Value;
            // }
            // Check the instance
            instance.ShouldBeOfType<Service>();
        }
    }
}
