/*
$v=true
$p=0
$d=Unit testing
$sa=Dependent compositions
$sa=Composition arguments
$sa=Root arguments
$h=A key benefit of dependency injection is testability: to test a service, replace its dependencies with deterministic test doubles. With Pure.DI, this substitution happens in the setup code and is verified at compile time. Put the bindings shared by the application and the tests into a `CompositionKind.Internal` setup, and let each composition — the production one and the test one — add its environment-specific bindings via `DependsOn(...)`.
$h=To use a mocking library such as _Moq_, bind a configured `Mock<T>` as a singleton, map the contract to `mock.Object`, and expose the mock itself as a composition root — the test then reaches the mock through that root to arrange behavior and verify calls.
$f=>[!TIP]
$f=>If you prefer to avoid mocking libraries, bind a hand-written fake in the test setup instead: `.Bind<IClock>().To<FakeClock>()`.
$r=Shouldly;Moq
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global
// ReSharper disable PartialTypeWithSinglePart
namespace Pure.DI.UsageTests.UseCases.UnitTestingScenario;

using Moq;
using Shouldly;
using Xunit;
using static CompositionKind;

// {
//# using Pure.DI;
//# using static Pure.DI.CompositionKind;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        // Bindings shared by the application and the tests
        DI.Setup("Shared", Internal)
            .Bind<IOrderService>().To<OrderService>();

        // The production composition uses the real clock
        DI.Setup(nameof(Composition))
            .DependsOn("Shared")
            .Singleton<SystemClock>()
            .Root<IOrderService>("OrderService");

        // The test composition binds a configured mock instead of the real clock
        // and exposes it as the "Clock" root so the test can access it
        DI.Setup(nameof(TestComposition))
            .DependsOn("Shared")
            .Singleton(_ => {
                // The test replaces the clock with a deterministic mock
                var clock = new Mock<IClock>();
                clock.SetupGet(i => i.Now)
                    .Returns(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
                return clock;
            }).Root<Mock<IClock>>("Clock")
            .Transient((Mock<IClock> mock) => mock.Object)
            .Root<IOrderService>("OrderService");

        // And verifies the service logic against the fixed time
        var composition = new TestComposition();
        var orderService = composition.OrderService;

        // An order placed 31 days before the mocked "now" is expired
        orderService.IsExpired(composition.Clock.Object.Now.AddDays(-31)).ShouldBeTrue();

        // An order placed 1 day before the mocked "now" is still valid
        orderService.IsExpired(composition.Clock.Object.Now.AddDays(-1)).ShouldBeFalse();
// }
        new Composition().OrderService.ShouldBeOfType<OrderService>();
        composition.SaveClassDiagram();
    }
}

// {
// The contract is public so that the mocking library can create a proxy for it
public interface IClock
{
    DateTimeOffset Now { get; }
}

// The real clock used by the application
class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IOrderService
{
    bool IsExpired(DateTimeOffset orderDate);
}

// The service under test knows nothing about test doubles
class OrderService(IClock clock) : IOrderService
{
    public bool IsExpired(DateTimeOffset orderDate) =>
        clock.Now - orderDate > TimeSpan.FromDays(30);
}
// }
