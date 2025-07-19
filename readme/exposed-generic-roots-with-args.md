#### Exposed generic roots with args

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
```bash
dotnet --list-sdk
```
- Create a net9.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add reference to NuGet package
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
```bash
dotnet add package Pure.DI
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif

  private OtherAssembly.CompositionWithGenericRootsAndArgsInOtherProject? _singleCompositionWithGenericRootsAndArgsInOtherProject53;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Program GetProgram(int id)
  {
    OtherAssembly.IMyGenericService<int> transIMyGenericService1;
    int localId45 = id;
    if (_root._singleCompositionWithGenericRootsAndArgsInOtherProject53 is null)
    {
      lock (_lock)
      {
        _root._singleCompositionWithGenericRootsAndArgsInOtherProject53 = new OtherAssembly.CompositionWithGenericRootsAndArgsInOtherProject();
      }
    }

    OtherAssembly.CompositionWithGenericRootsAndArgsInOtherProject localInstance_1182D12746 = _root._singleCompositionWithGenericRootsAndArgsInOtherProject53;
    transIMyGenericService1 = localInstance_1182D12746.GetMyService<int>(localId45);
    return new Program(transIMyGenericService1);
  }
}
```


