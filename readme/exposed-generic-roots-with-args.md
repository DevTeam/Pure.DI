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
interface IDependency;

class Dependency : IDependency;

interface IService;

class Service(IDependency dependency) : IService;

class Program(IService service, IMyGenericService<int> myService)
{
    public IService Service { get; } = service;

    public void DoSomething(int value) => myService.DoSomething(value);
}

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    .RootArg<int>("id")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    // Binds to exposed composition roots from other project
    .Bind().As(Lifetime.Singleton).To<CompositionWithGenericRootsAndArgsInOtherProject>()
    .Root<Program>("GetProgram");

var composition = new Composition();
var program = composition.GetProgram(33);
program.DoSomething(99);
```

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly object _lock;

  private Integration.CompositionWithGenericRootsAndArgsInOtherProject? _singletonCompositionWithGenericRootsAndArgsInOtherProject42;

  [OrdinalAttribute(10)]
  public Composition()
  {
    _root = this;
    _lock = new object();
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Program GetProgram(int id)
  {
    if (_root._singletonCompositionWithGenericRootsAndArgsInOtherProject42 == null)
    {
        lock (_lock)
        {
            if (_root._singletonCompositionWithGenericRootsAndArgsInOtherProject42 == null)
            {
                _root._singletonCompositionWithGenericRootsAndArgsInOtherProject42 = new Integration.CompositionWithGenericRootsAndArgsInOtherProject();
            }
        }
    }

    Integration.IMyGenericService<int> transientIMyGenericService2;
    {
        int localId1 = id;
        Integration.CompositionWithGenericRootsAndArgsInOtherProject localValue2 = _root._singletonCompositionWithGenericRootsAndArgsInOtherProject42!;
        transientIMyGenericService2 = localValue2.GetMyService<int>(localId1);
    }

    return new Program(new Service(new Dependency()), transientIMyGenericService2);
  }
}
```


