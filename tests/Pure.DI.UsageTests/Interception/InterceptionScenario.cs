/*
$v=true
$p=1
$d=Interception
$h=Interception allows you to enrich or change the behavior of a certain set of objects from the object graph being created without changing the code of the corresponding types.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable HeapView.PossibleBoxingAllocation
namespace Pure.DI.UsageTests.Interception.InterceptionScenario;

using Castle.DynamicProxy;
using Shouldly;
using Xunit;

// {
public interface IService { string GetMessage(); }

internal class Service : IService 
{
    public string GetMessage() => "Hello World";
}

internal partial class Composition: IInterceptor
{
    private static readonly ProxyGenerator ProxyGenerator = new();

    private partial T OnDependencyInjection<T>(in T value, object? tag, Lifetime lifetime)
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
        // ToString = On
        // FormatCode = On
// {            
        // OnDependencyInjection = On
        // OnDependencyInjectionContractTypeNameRegularExpression = IService
        DI.Setup("Composition")
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.GetMessage().ShouldBe("Hello World !!!");
// }
        TestTools.SaveClassDiagram(composition, nameof(InterceptionScenario));
    }
}