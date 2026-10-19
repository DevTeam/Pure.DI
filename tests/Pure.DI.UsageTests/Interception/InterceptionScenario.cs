/*
$v=true
$p=1
$d=Interception
$sa=Decorator
$sa=Advanced interception
$h=Interception lets you enrich or change the behavior of a certain set of objects from the object graph being created without changing the code of the corresponding types.
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
$f=
$f=This is the option for cross-cutting behavior over contracts you do not control - a third-party client, a generated API, a legacy interface - where writing a decorator per contract is not practical. The hook is a partial method on the composition, so the whole policy lives in one place.
$f=The generated graph is unchanged: every dependency is built as usual and then passed through `OnDependencyInjection`. An interceptor therefore sees the real instance and can delegate to it, but it also runs on the hot path of every resolution.
$f=>[!WARNING]
$f=>Proxies are only created for reference types, and the interceptor must skip value types explicitly. Enable the hook for a whole graph and every interface gets a proxy, including infrastructure you never meant to wrap - narrow it with `OnDependencyInjectionContractTypeNameWildcard` as this example does.
$f=
$f=Prefer [Decorator](decorator.md) for your own contracts, and [Advanced interception](advanced-interception.md) when the proxy creation itself becomes the bottleneck.
$r=Shouldly;Castle.DynamicProxy
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable HeapView.PossibleBoxingAllocation
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Interception.InterceptionScenario;

using System.Runtime.CompilerServices;
using Castle.DynamicProxy;
using Shouldly;
using Xunit;

// {
//# using System.Runtime.CompilerServices;
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {    
        // OnDependencyInjection = On
        // OnDependencyInjectionContractTypeNameWildcard = *IGreeter
        DI.Setup(nameof(Composition))
            .Bind().To<Greeter>()
            .Root<IGreeter>("Greeter");

        var composition = new Composition();
        var greeter = composition.Greeter;

        // The greeting is modified by the interceptor
        greeter.Greet("World").ShouldBe("Hello World !!!");
// }
        composition.SaveClassDiagram();
    }
}

// {
public interface IGreeter
{
    string Greet(string name);
}

class Greeter : IGreeter
{
    public string Greet(string name) => $"Hello {name}";
}

partial class Composition : IInterceptor
{
    private static readonly ProxyGenerator ProxyGenerator = new();

    // Intercepts the instantiation of services to wrap them in a proxy
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        // Proxying is only possible for reference types (interfaces, classes)
        if (typeof(T).IsValueType)
        {
            return value;
        }

        // Creates a proxy that delegates calls to the 'value' object
        // and passes them through the 'this' interceptor
        return (T)ProxyGenerator.CreateInterfaceProxyWithTargetInterface(
            typeof(T),
            value,
            this);
    }

    // Logic performed when a method on the proxy is called
    public void Intercept(IInvocation invocation)
    {
        // Executes the original method
        invocation.Proceed();

        // Enhances the result of the Greet method
        if (invocation.Method.Name == nameof(IGreeter.Greet)
            && invocation.ReturnValue is string message)
        {
            invocation.ReturnValue = $"{message} !!!";
        }
    }
}
// }