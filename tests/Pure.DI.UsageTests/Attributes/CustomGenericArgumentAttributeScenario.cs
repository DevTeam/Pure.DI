/*
$v=true
$p=12
$d=Custom generic argument attribute
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedTypeParameter
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable InconsistentNaming

namespace Pure.DI.UsageTests.Attributes.CustomGenericArgumentAttributeScenario;

using Shouldly;
using Xunit;

// {
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
class MyGenericTypeArgumentAttribute : Attribute;

[MyGenericTypeArgument]
interface TTMy;

interface IDependency<T>;

class Dependency<T> : IDependency<T>;

interface IService
{
    IDependency<int> IntDependency { get; }

    IDependency<string> StringDependency { get; }
}

class Service(
    IDependency<int> intDependency,
    IDependency<string> stringDependency)
    : IService
{
    public IDependency<int> IntDependency { get; } = intDependency;

    public IDependency<string> StringDependency { get; } = stringDependency;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup(nameof(Composition))
            // Registers custom generic argument
            .GenericTypeArgumentAttribute<MyGenericTypeArgumentAttribute>()
            .Bind<IDependency<TTMy>>().To<Dependency<TTMy>>()
            .Bind<IService>().To<Service>()

            // Composition root
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.IntDependency.ShouldBeOfType<Dependency<int>>();
        service.StringDependency.ShouldBeOfType<Dependency<string>>();
// }
        composition.SaveClassDiagram();
    }
}