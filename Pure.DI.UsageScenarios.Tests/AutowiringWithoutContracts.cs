// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
namespace Pure.DI.UsageScenarios.Tests
{
    using Xunit;
    
    public class DependencyInjectionWithImplementations
    {
        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=99
        // $description=Dependency Injection with referencing implementations
        // $header=Autowiring automatically injects dependencies based on implementations even if it is not defined in the configuration chain. :warning: But this approach is not recommended. When you follow the dependency inversion principle you want to make sure that you do not depend on anything concrete.
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IService>().To<Service>();
            
            var instance = DependencyInjectionWithImplementationsDI.Resolve<IService>();
        }

        public class Dependency { }

        public interface IService { }

        public class Service : IService
        {
            public Service(Dependency dependency) { }
        }
        // }
    }
}