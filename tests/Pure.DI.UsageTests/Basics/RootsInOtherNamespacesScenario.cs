/*
$v=true
$t=Basics
$p=35
$d=Roots in other namespaces
$sa=Roots with filter
$h=`Roots<T>` discovers implementations that are visible from the setup location, so an implementation declared in a namespace that is not imported is invisible and the compilation fails with `DIE016`.
$h=Passing `searchInCompilation: true` searches every type declared in the current assembly instead, so `EmailService` is found even though nothing imports `Impl`.
$h=The full search is opt-in because it walks the whole assembly; the default lookup from the setup location stays the fast path.
$f=Prefer a `using` directive when the namespace is known: it keeps the generated set explicit and costs nothing at generation time.
$f=Reach for `searchInCompilation: true` when the setup lives in its own namespace and plugin implementations are spread across the assembly.
$f=The option is available on `Builders<T>` as well and combines with `filter`.
$f=When no type matches at all, the message names the implementations that exist in the compilation but are not visible and suggests the fix.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable UnusedType.Global
namespace Pure.DI.UsageTests.Basics.RootsInOtherNamespacesScenario
{
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
// {
            DI.Setup(nameof(Composition))
                .Roots<INotificationService>("My{type}", searchInCompilation: true);

            var composition = new Composition();
            composition.MyEmailService.ShouldBeOfType<Impl.EmailService>();
// }
        }
    }

    // {
    public interface INotificationService;

    namespace Impl
    {
        public class EmailService : INotificationService;
    }
    // }
}
