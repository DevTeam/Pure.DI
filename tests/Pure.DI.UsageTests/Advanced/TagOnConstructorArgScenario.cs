/*
$v=true
$p=6
$d=Tag on a constructor argument
$h=The wildcards ‘*’ and ‘?’ are supported.
$f=> [!WARNING]
$f=> Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedTypeParameter
#pragma warning disable CS9113 // Parameter is unread.
// {
namespace Pure.DI.UsageTests.Advanced.OnConstructorArgScenario;

// }

using Pure.DI;
using UsageTests;
using Xunit;

// {
interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Consumer<T>(IDependency myDep)
{
    public IDependency Dependency { get; } = myDep;
}
        
interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }
}

class Service(
    IDependency dependency1,
    Consumer<string> consumer)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;
    
    public IDependency Dependency2 => consumer.Dependency;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {            
        DI.Setup(nameof(Composition))
            .Bind(Tag.OnConstructorArg<Service>("dependency1"))
                .To<AbcDependency>()
            .Bind(Tag.OnConstructorArg<Consumer<TT>>("myDep"))
                .To<XyzDependency>()
            .Bind<IService>().To<Service>()
            
            // Specifies to create the composition root named "Root"
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency1.ShouldBeOfType<AbcDependency>();
        service.Dependency2.ShouldBeOfType<XyzDependency>();
// }            
        composition.SaveClassDiagram();
    }
}