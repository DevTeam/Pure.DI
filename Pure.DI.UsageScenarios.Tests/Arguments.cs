// ReSharper disable IdentifierTypo
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable EmptyConstructor
// ReSharper disable UnusedVariable
// ReSharper disable ArrangeNamespaceBody
namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class Arguments
    {
        [Fact]
        // $visible=true
        // $tag=1 Basics
        // $priority=01
        // $description=Resolution arguments
        // {
        public void Run()
        {
            DI.Setup()
                .Arg<string>()
                .Arg<int>("indexVal")
                .Arg<int>()
                .Bind<IDependency>().To<Dependency>()
                .Bind<IService>().To<SomeService>();

            // Resolve an instance
            var instance = ArgumentsDI.Resolve<IService>("some setting", 33, 99);

            // Check the optional dependency
            instance.State.ShouldBe("some setting");
            instance.Dependency.Index.ShouldBe(33);
        }
        
        public class Dependency : IDependency
        {
            public Dependency([Tag("indexVal")] int index, int notTagged)
            {
                Index = index;
            }
            
            public int Index { get; set; }
        }

        public class SomeService: IService
        {
            // There is no registered dependency for parameter "state" of type "string",
            // but parameter "state" has a nullable annotation
            public SomeService(IDependency dependency, string state)
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