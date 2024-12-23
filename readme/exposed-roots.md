#### Exposed roots

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/ExposedRootsScenario.cs)

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
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
using static Pure.DI.Lifetime;
using Pure.DI.Integration;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionInOtherProject>()
    .Root<Program>("Program");

var composition = new Composition();
var program = composition.Program;
program.DoSomething();

partial class Program(IMyService myService)
{
    public void DoSomething() => myService.DoSomething();
}
```

> [!IMPORTANT]
> At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.



