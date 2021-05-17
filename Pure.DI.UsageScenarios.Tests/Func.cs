namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class Func
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=3 BCL types
            // $priority=01
            // $description=Func
            // $header=_Func<>_ helps when a logic needs to inject some type of instances on-demand or solve circular dependency issues.
            // {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<Service>()
                .Bind<CompositionRoot<Func<IService>>>().To<CompositionRoot<Func<IService>>>();

            // Resolve function to create instances
            var factory = FuncDI.Resolve<CompositionRoot<Func<IService>>>().Root;

            // Resolve few instances
            var instance1 = factory();
            var instance2 = factory();
            // }
            // Check each instance
            instance1.ShouldBeOfType<Service>();
            instance2.ShouldBeOfType<Service>();
        }
    }
}
