// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class MethodsAccessibility
    {
        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=11
        // $description=Methods Accessibility
        // $header=It is possible can change an accessibility of level all resolving generated methods by special hint in the comment before the _Setup()_ method as in the following sample:
        // $footer=Accessibility levels:
        // $footer=- public (by default)
        // $footer=- internal
        // $footer=- private
        // {
        public void Run()
        {
            // Accessibility = private
            DI.Setup("MyTestComposer")
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>();

            var instance = MyTestComposer.Service;
            instance.ShouldBeOfType<Service>();
        }
        // }
    }
    
        // {

        internal static partial class MyTestComposer
        {
            public static IService Service => ResolveIService();
        }
        // }
}
