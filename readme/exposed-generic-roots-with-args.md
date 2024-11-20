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
class Program(IMyGenericService<int> myService)
{
    public void DoSomething(int value) => myService.DoSomething(value);
}

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    .RootArg<int>("id")
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

  private Integration.CompositionWithGenericRootsAndArgsInOtherProject? _singletonCompositionWithGenericRootsAndArgsInOtherProject40;

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
    if (_root._singletonCompositionWithGenericRootsAndArgsInOtherProject40 is null)
    {
      lock (_lock)
      {
        if (_root._singletonCompositionWithGenericRootsAndArgsInOtherProject40 is null)
        {
          _root._singletonCompositionWithGenericRootsAndArgsInOtherProject40 = new Integration.CompositionWithGenericRootsAndArgsInOtherProject();
        }
      }
    }

    Integration.IMyGenericService<int> transientIMyGenericService1;
    int localId1 = id;
    Integration.CompositionWithGenericRootsAndArgsInOtherProject localInstance_1182D1272 = _root._singletonCompositionWithGenericRootsAndArgsInOtherProject40!;
    transientIMyGenericService1 = localInstance_1182D1272.GetMyService<int>(localId1);
    return new Program(transientIMyGenericService1);
  }
}
```


