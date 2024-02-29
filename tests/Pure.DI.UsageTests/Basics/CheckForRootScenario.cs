/*
$v=true
$p=16
$d=Check for a root
$h=Sometimes you need to check if you can get the root of a composition using the _Resolve_ method before calling it, this example will show you how to do it:
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.CheckForRootScenario;

using Shouldly;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get;}
}

class Service : IService
{
    [Tag("MyDep")]
    public required IDependency Dependency { get; init; }
}

partial class Composition
{
    private static readonly HashSet<(Type type, object? tag)> Roots = [];

    // Check that the root can be resolved by Resolve methods
    internal static bool HasRoot(Type type, object? key = default) =>
        Roots.Contains((type, key));
        
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            // Specifies to use the partial OnNewRoot method
            // to register each root
            .Hint(Hint.OnNewRoot, "On")
            
            .Bind<IDependency>("MyDep").To<Dependency>()
            .Bind<IService>().To<Service>()
            
            .Root<IDependency>(tag: "MyDep")
            .Root<IService>("Root");

    // Adds a new root to the hash set 
    private static partial void OnNewRoot<TContract, T>(
        IResolver<Composition, TContract> resolver,
        string name,
        object? tag,
        Lifetime lifetime) => 
        Roots.Add((typeof(TContract), tag));
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        Composition.HasRoot(typeof(IService)).ShouldBeTrue();
        Composition.HasRoot(typeof(IDependency), "MyDep").ShouldBeTrue();
        
        Composition.HasRoot(typeof(IDependency)).ShouldBeFalse();
        Composition.HasRoot(typeof(IComparable)).ShouldBeFalse();
        
// }            
        new Composition().SaveClassDiagram();
    }
}