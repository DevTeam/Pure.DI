// ReSharper disable IdentifierTypo
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable EmptyConstructor
// ReSharper disable UnusedVariable
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable MemberHidesStaticFromOuterClass
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
        // $header=The sample below demonstrates how to specify resolution arguments that will be added to all resolving methods.
        // $header=:warning: It is important to know that these arguments are not available with delayed resolution (in cases like Func outside constructors and etc.), they can only be used in the static composition object graph.
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
            var instance = ArgumentsDI.Resolve<IService>("some settings", 33, 99);

            instance.State.ShouldBe("some settings 99");
            instance.Dependency.Index.ShouldBe(33 + 99);
        }
        
        public class Dependency : IDependency
        {
            public Dependency([Tag("indexVal")] int index, int notTagged)
            {
                Index = index + notTagged;
            }
            
            public int Index { get; set; }
        }

        public class SomeService: IService
        {
            // There is no registered dependency for parameter "state" of type "string",
            // but parameter "state" has a nullable annotation
            public SomeService(IDependency dependency, string state, int notTagged)
            {
                Dependency = dependency;
                State = $"{state} {notTagged}";
            }

            public IDependency Dependency { get; }

            public string State { get; }
        }
        // }
    }
}