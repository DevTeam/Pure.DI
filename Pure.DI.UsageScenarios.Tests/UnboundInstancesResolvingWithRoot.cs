// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Xunit;
    
    public class UnboundInstancesResolvingWithRoot
    {
        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=100
        // $description=Unbound instances resolving with root specified
        // {
        public void Run()
        {
            DI.Setup()
                .Root<Service>();

            var instance = UnboundInstancesResolvingWithRootDI.Resolve<Service>();
        }

        public class Dependency { }

        public class Service
        {
            public Service(Dependency dependency) { }
        }
        // }
    }
}