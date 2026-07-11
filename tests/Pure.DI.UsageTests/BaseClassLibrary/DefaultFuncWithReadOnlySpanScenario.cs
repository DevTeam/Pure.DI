/*
$v=true
$p=98
$d=Default Func with ReadOnlySpan
$sa=Span and ReadOnlySpan
$sa=Func
$h=Pure.DI can generate the standard `Func<ReadOnlySpan<char>, T>` factory automatically. The runtime span argument is kept as a local value inside the generated delegate invocation, so no manual `ctx.Override(...)` call and no `lock (ctx.Lock)` block are required.
$f=Use this default `Func` binding when the standard delegate shape is enough. Use a custom delegate factory with explicit `ctx.Override<T>(...)` only when you need a custom delegate type or additional factory logic.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global

namespace Pure.DI.UsageTests.BCL.DefaultFuncWithReadOnlySpanScenario;

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

            // Composition root
            .Root<Func<ReadOnlySpan<char>, Parser>>("ParserFactory");

        var composition = new Composition();
        var parser = composition.ParserFactory("Hello".AsSpan());

        parser.Value.ShouldBe("Hello");
// }
        composition.SaveClassDiagram();
    }
}

// {
class Parser
{
    private string _value = "";

    [Ordinal]
    public void Initialize(ReadOnlySpan<char> text)
    {
        _value = text.ToString();
    }

    public string Value => _value;
}
// }
