// ReSharper disable IdentifierTypo
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable EmptyConstructor
// ReSharper disable UnusedVariable
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class DefaultParamsInjection
    {
        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=06
        // $description=Injection of default parameters
        // {
        public void Run()
        {
            DI.Setup()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<SomeService>();

            // Resolve an instance
            var instance = DefaultParamsInjectionDI.Resolve<IService>();

            // Check the optional dependency
            instance.State.ShouldBe("empty");
        }

        public class SomeService: IService
        {
            // "state" dependency is not resolved here but it has the default value "empty"
            public SomeService(IDependency dependency, string state = "empty")
            {
                Dependency = dependency;
                State = state;
            }

            public IDependency Dependency { get; }

            public string State { get; }
        }
        // }
    }
}