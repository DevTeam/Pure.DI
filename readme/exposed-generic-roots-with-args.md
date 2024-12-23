#### Exposed generic roots with args

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/ExposedGenericRootsWithArgsScenario.cs)

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionWithGenericRootsAndArgsInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Hint(Hint.Resolve, "Off")
            .RootArg<int>("id")
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyGenericService<TT>>()
            .Root<IMyGenericService<TT>>("GetMyService", kind: RootKinds.Exposed);
}
```


```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using Pure.DI.Integration;

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    .RootArg<int>("id")
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionWithGenericRootsAndArgsInOtherProject>()
    .Root<Program>("GetProgram");

var composition = new Composition();
var program = composition.GetProgram(33);
program.DoSomething(99);

partial class Program(IMyGenericService<int> myService)
{
    public void DoSomething(int value) => myService.DoSomething(value);
}
```



