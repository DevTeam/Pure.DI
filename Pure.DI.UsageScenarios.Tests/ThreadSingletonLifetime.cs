// ReSharper disable AccessToDisposedClosure
namespace Pure.DI.UsageScenarios.Tests
{
    using System.Threading;
    using Shouldly;
    using Xunit;

    public class ThreadSingletonLifetime
    {
        [Fact]
        // $visible=true
        // $tag=2 Lifetimes
        // $priority=10
        // $description=Per thread singleton
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                // Bind an interface to an implementation using the per thread singleton lifetime
                .Bind<IService>().As(Lifetime.PerThread).To<Service>();

            // Resolve the singleton twice
            var instance1 = ThreadSingletonLifetimeDI.Resolve<IService>();
            var instance2 = ThreadSingletonLifetimeDI.Resolve<IService>();
            IService? instance3 = null;
            IService? instance4 = null;

            var finish = new ManualResetEvent(false);
            var newThread = new Thread(() =>
            {
                instance3 = ThreadSingletonLifetimeDI.Resolve<IService>();
                instance4 = ThreadSingletonLifetimeDI.Resolve<IService>();
                finish.Set();
            });

            newThread.Start();
            finish.WaitOne();

            // Check that instances resolved in a main thread are equal
            instance1.ShouldBe(instance2);

            // Check that instance resolved in a new thread is not null
            instance3!.ShouldNotBeNull();

            // Check that instances resolved in different threads are not equal
            instance1.ShouldNotBe(instance3!);

            // Check that instances resolved in a new thread are equal
            instance4!.ShouldBe(instance3!);
        }
        // }
    }
}
