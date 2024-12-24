#### Exposed roots via root arg

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/ExposedRootsViaRootArgScenario.cs)

Composition roots from other assemblies or projects can be used as a source of bindings passed through root arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exposed);
}
```


```c#
using Pure.DI;
using OtherAssembly;

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    // Binds to exposed composition roots from other project
    .RootArg<CompositionInOtherProject>("baseComposition")
    .Root<Program>("GetProgram");

var baseComposition = new CompositionInOtherProject();
var composition = new Composition();
var program = composition.GetProgram(baseComposition);
program.DoSomething();

partial class Program(IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
```



