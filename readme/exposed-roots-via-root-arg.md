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
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

class Program(IService service, IMyService myService)
{
    public IService Service { get; } = service;

    public void DoSomething() => myService.DoSomething();
}

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    // Binds to exposed composition roots from other project
    .RootArg<CompositionInOtherProject>("baseComposition")
    .Root<Program>("GetProgram");

var baseComposition = new CompositionInOtherProject();
var composition = new Composition();
var program = composition.GetProgram(baseComposition);
program.DoSomething();
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;

  [OrdinalAttribute(10)]
  public Composition()
  {
    _root = this;
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Program GetProgram(Integration.CompositionInOtherProject baseComposition)
  {
    Integration.IMyService transientIMyService2;
    {
        Integration.CompositionInOtherProject localValue5 = baseComposition;
        transientIMyService2 = localValue5.MyService;
    }

    return new Program(new Service(new Dependency()), transientIMyService2);
  }
}
```


