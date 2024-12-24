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
using OtherAssembly;

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

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed
- Create a net9.0 (or later) console application
- Add reference to NuGet package
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
- Copy the example code into the _Program.cs_ file

You are ready to run the example!

</details>

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly Lock _lock;

  private OtherAssembly.CompositionWithGenericRootsAndArgsInOtherProject? _singletonCompositionWithGenericRootsAndArgsInOtherProject44;

  [OrdinalAttribute(128)]
  public Composition()
  {
    _root = this;
    _lock = new Lock();
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Program GetProgram(int id)
  {
    if (_root._singletonCompositionWithGenericRootsAndArgsInOtherProject44 is null)
    {
      using (_lock.EnterScope())
      {
        if (_root._singletonCompositionWithGenericRootsAndArgsInOtherProject44 is null)
        {
          _root._singletonCompositionWithGenericRootsAndArgsInOtherProject44 = new OtherAssembly.CompositionWithGenericRootsAndArgsInOtherProject();
        }
      }
    }

    OtherAssembly.IMyGenericService<int> transientIMyGenericService1;
    int localId3 = id;
    OtherAssembly.CompositionWithGenericRootsAndArgsInOtherProject localInstance_1182D1274 = _root._singletonCompositionWithGenericRootsAndArgsInOtherProject44;
    transientIMyGenericService1 = localInstance_1182D1274.GetMyService<int>(localId3);
    return new Program(transientIMyGenericService1);
  }
}
```


