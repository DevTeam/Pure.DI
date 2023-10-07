/*
$v=true
$p=99
$d=Func with arguments
$f=To distinguish between several different bindings of the same type you can use tags.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable VariableHidesOuterVariable
namespace Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IDependency
{
    int Id { get; }
}

class Dependency : IDependency
{
    public Dependency(IClock clock, int id) => 
        Id = id;

    public int Id { get; }
}

interface IService
{
    ImmutableArray<IDependency> Dependencies { get; }
}

class Service : IService
{
    public Service(Func<int, IDependency> dependencyFactory) =>
        Dependencies = Enumerable
            .Range(0, 10)
            .Select((_, index) => dependencyFactory(index))
            .ToImmutableArray();

    public ImmutableArray<IDependency> Dependencies { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {    
        // Declares the "dependencyId" variable to setup binding
        var dependencyId = default(int);
        DI.Setup("Composition")
            .Bind<IClock>().As(Lifetime.Singleton).To<Clock>()
            // Binds int to dependencyId
            .Bind<int>().To(_ => dependencyId)
            .Bind<Func<int, IDependency>>().To(ctx =>
                // The name of the Lambda function argument must match
                // the variable in the binding, in our case it is "dependencyId"
                new Func<int, IDependency>(dependencyId =>
                {
                    // Builds up an instance of type Dependency
                    // with all necessary dependencies,
                    // including those of type int,
                    // referring to the variable "dependencyId"
                    ctx.Inject<Dependency>(out var dependency);
                    return dependency;
                }))
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Length.ShouldBe(10);
        service.Dependencies[3].Id.ShouldBe(3);
// }            
        composition.SaveClassDiagram();
    }
}