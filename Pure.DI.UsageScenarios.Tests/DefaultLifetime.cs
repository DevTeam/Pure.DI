// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;
    using static Lifetime;

    public class DefaultLifetime
    {
        [Fact]
        
        // $visible=true
        // $tag=2 Lifetimes
        // $priority=01
        // $description=Default lifetime
        // {
        public void Run()
        {
            DI.Setup()
                // Makes Singleton as default lifetime
                .Default(Singleton)
                    .Bind<IDependency>().To<Dependency>()
                // Makes Transient as default lifetime
                .Default(Transient)
                    .Bind<IService>().To<Service>();
            
            // Resolve the singleton twice
            var instance = DefaultLifetimeDI.Resolve<IService>();

            // Check that instances are equal
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
