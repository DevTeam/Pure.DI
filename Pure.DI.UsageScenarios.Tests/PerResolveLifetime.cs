// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;
    using static Lifetime;

    public class PerResolveLifetime
    {
        [Fact]
        // $visible=true
        // $tag=2 Lifetimes
        // $priority=01
        // $description=Per resolve
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IDependency>().As(PerResolve).To<Dependency>()
                // Use the Singleton lifetime
                .Bind<IService>().To<Service>();

            var instance = PerResolveLifetimeDI.Resolve<IService>();

            // Check that dependencies are equal
            instance.Dependency1.ShouldBe(instance.Dependency2);
        }

        public interface IDependency { }

        public class Dependency : IDependency { }

        public interface IService
        {
            IDependency Dependency1 { get; }
            
            IDependency Dependency2 { get; }
        }

        public class Service : IService
        {
            public Service(IDependency dependency1, IDependency dependency2)
            {
                Dependency1 = dependency1;
                Dependency2 = dependency2;
            }

            public IDependency Dependency1 { get; }
            
            public IDependency Dependency2 { get; }
        }
        // }
    }
}
