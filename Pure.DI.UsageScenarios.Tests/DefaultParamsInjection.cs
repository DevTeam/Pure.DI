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
            instance.State.ShouldBe("my default value");
        }

        public class SomeService: IService
        {
            // There is no registered dependency for parameter "state" of type "string", but constructor has the default parameter value "my default value"
            public SomeService(IDependency dependency, string state = "my default value")
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