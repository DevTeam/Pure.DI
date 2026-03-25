/*
$v=true
$p=11
$d=IsLockRequired
$h=`IsLockRequired` indicates whether a lock is required for thread-safe operations in the current context. This property is useful when you need to conditionally synchronize based on thread safety requirements.
$h=Use this when custom factory logic must respect thread-safety semantics of generated code.
$f=Limitations: avoid adding business logic inside lock-aware factories; use it only for synchronization concerns.
$f=See also: [ThreadSafe hint](threadsafe-hint.md), [Factory](factory.md).
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedPositionalProperty.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.IsLockRequiredScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ThreadSafe = On (default)
// {
        var composition = new Composition();

        var service = composition.Service;
        service.Locked.ShouldBeTrue();

        var singletonService = composition.SingletonService;
        singletonService.Locked.ShouldBeFalse();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IService
{
    bool Locked { get; }
}

class Service(bool lockRequired) : IService
{
    public bool Locked => lockRequired;
}

partial class Composition
{
    private void Setup() =>
// }
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Hint(Hint.ThreadSafe, "On")
            .Bind().To(ctx =>
            {
                // In a thread-safe context, IsLockRequired is true
                // Use it to conditionally lock the context
                if (ctx.IsLockRequired)
                {
                    lock (ctx.Lock)
                    {
                        return new Service(ctx.IsLockRequired);
                    }
                }

                return new Service(ctx.IsLockRequired);
            })
            .Bind(Tag.Single).As(Lifetime.Singleton).To((IService service) => service)
            .Root<IService>(nameof(Service))
            .Root<IService>(nameof(SingletonService), Tag.Single);
}
// }
