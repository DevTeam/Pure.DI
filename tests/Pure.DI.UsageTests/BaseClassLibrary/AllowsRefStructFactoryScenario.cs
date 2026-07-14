/*
$v=true
$t=BaseClassLibrary
$t=HighPerformance
$p=99
$p=HighPerformance:3
$d=Allows ref struct factory
$sa=Span and ReadOnlySpan
$sa=Factory
$sa=Thread-safe overrides
$h=A delegate factory can accept stack-only values when the value is consumed immediately inside the same invocation. For custom generic delegate APIs that use `where T : allows ref struct`, pass the delegate argument through `ctx.Override<T>(...)` or `ctx.Let<T>(...)`, resolve the target immediately, and keep the override plus injection inside `lock (ctx.Lock)` when thread safety is enabled.
$f=This manual factory pattern keeps `ReadOnlySpan<T>` and other stack-only values inside the current synchronous frame. Pure.DI reports `DIE049` if the value is captured by a nested or returned delegate, and `DIW013` if the stack-only override is not synchronized while thread safety is enabled.
$f=When the standard delegate shape is enough, prefer the generated default `Func<ReadOnlySpan<char>, T>` binding. It uses local values in the generated delegate invocation and does not require a manual `lock`.
$f=The fluent contract API accepts ref-like type arguments in `Bind<T>()`, `RootBind<T>()`, `Root<T>()`, and `DefaultLifetime<T>()`. The `Transient<T>()`, `PerResolve<T>()`, and `PerBlock<T>()` shortcuts support ref-like implementations and factory results because generated instances remain local to root execution.
$f=`Singleton<T>()` and `Scoped<T>()` keep their implementation and factory-result type heap-safe. Their parameterized factory overloads accept ref-like dependency types so Pure.DI can apply its stored-lifetime validation and report `DIE046` instead of failing earlier with the C# generic-argument diagnostic `CS9244`.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Pure.DI.UsageTests.BCL.AllowsRefStructFactoryScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
//# using System;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Disable Resolve methods to keep the public API minimal
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .Bind<ParserFactory<ReadOnlySpan<char>>>().To(ctx => new ParserFactory<ReadOnlySpan<char>>(text =>
            {
                lock (ctx.Lock)
                {
                    ctx.Override<ReadOnlySpan<char>>(text);
                    ctx.Inject<Parser<ReadOnlySpan<char>>>(out var parser);
                    return parser.Initialized;
                }
            }))

            // Composition root
            .Root<ParserFactory<ReadOnlySpan<char>>>("ParserFactory");

        var composition = new Composition();
        var initialized = composition.ParserFactory("Hello".AsSpan());

        initialized.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}

// {
delegate bool ParserFactory<in T>(T text)
    where T : allows ref struct;

class Parser<T>
    where T : allows ref struct
{
    private bool _initialized;

    [Ordinal]
    public void Initialize(T text)
    {
        _ = text;
        _initialized = true;
    }

    public bool Initialized => _initialized;
}
// }
