/*
$v=true
$p=1
$d=Interception
$h=Interception allows you to enrich or change the behavior of a certain set of objects from the object graph being created without changing the code of the corresponding types.
$f=Using an intercept gives you the ability to add end-to-end functionality such as:
$f=
$f=- Logging
$f=
$f=- Action logging
$f=
$f=- Performance monitoring
$f=
$f=- Security
$f=
$f=- Caching
$f=
$f=- Error handling
$f=
$f=- Providing resistance to failures, etc.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable HeapView.PossibleBoxingAllocation
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Interception.InterceptionScenario;

using Castle.DynamicProxy;
using Shouldly;
using Xunit;

// {
public interface IService { string GetMessage(); }

class Service : IService 
{
    public string GetMessage() => "Hello World";
}

partial class Composition: IInterceptor
{
    private static readonly ProxyGenerator ProxyGenerator = new();

    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        if (typeof(T).IsValueType)
        {
            return value;
        }

        return (T)ProxyGenerator.CreateInterfaceProxyWithTargetInterface(
            typeof(T),
            value,
            this);
    }

    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();
        if (invocation.Method.Name == nameof(IService.GetMessage)
            && invocation.ReturnValue is string message)
        {
            invocation.ReturnValue = $"{message} !!!";
        }
    }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {    
        // OnDependencyInjection = On
        // OnDependencyInjectionContractTypeNameRegularExpression = IService
        DI.Setup(nameof(Composition))
            .RootBind<IService>("Root").To<Service>();

        var composition = new Composition();
        var service = composition.Root;
        service.GetMessage().ShouldBe("Hello World !!!");
// }
        composition.SaveClassDiagram();
    }
}