﻿/*
$v=true
$p=211
$d=Exposed generic roots with args
$h=Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
$h=```c#
$h=public partial class CompositionWithGenericRootsAndArgsInOtherProject
$h={
$h=    private static void Setup() =>
$h=        DI.Setup()
$h=            .Hint(Hint.Resolve, "Off")
$h=            .RootArg<int>("id")
$h=            .Bind().As(Lifetime.Singleton).To<MyDependency>()
$h=            .Bind().To<MyGenericService<TT>>()
$h=            .Root<IMyGenericService<TT>>("GetMyService", kind: RootKinds.Exposed);
$h=}
$h=```
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeTypeModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.ExposedGenericRootsWithArgsScenario;

using OtherAssembly;
using Pure.DI;
using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
//# using OtherAssembly;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        DI.Setup(nameof(Composition))
            .Hint(Hint.Resolve, "Off")
            .RootArg<int>("id")
            // Binds to exposed composition roots from other project
            .Bind().As(Singleton).To<CompositionWithGenericRootsAndArgsInOtherProject>()
            .Root<Program>("GetProgram");

        var composition = new Composition();
        var program = composition.GetProgram(33);
        program.DoSomething(99);
// }
    }
}

// {
partial class Program(IMyGenericService<int> myService)
{
    public void DoSomething(int value) => myService.DoSomething(value);
}
// }