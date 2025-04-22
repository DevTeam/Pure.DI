#### Exposed generic roots

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exposed` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
    DI.Setup()
        .Hint(Hint.Resolve, "Off")
        .Bind().To(_ => 99)
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
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionWithGenericRootsInOtherProject>()
    .Root<Program>("Program");

var composition = new Composition();
var program = composition.Program;
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

> [!IMPORTANT]
> At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly Lock _lock;

  private OtherAssembly.CompositionWithGenericRootsInOtherProject? _singletonCompositionWithGenericRootsInOtherProject51;

  [OrdinalAttribute(256)]
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

  public Program Program
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      if (_root._singletonCompositionWithGenericRootsInOtherProject51 is null)
      {
        using (_lock.EnterScope())
        {
          if (_root._singletonCompositionWithGenericRootsInOtherProject51 is null)
          {
            _root._singletonCompositionWithGenericRootsInOtherProject51 = new OtherAssembly.CompositionWithGenericRootsInOtherProject();
          }
        }
      }

      OtherAssembly.IMyGenericService<int> transientIMyGenericService1;
      OtherAssembly.CompositionWithGenericRootsInOtherProject localInstance_1182D12741 = _root._singletonCompositionWithGenericRootsInOtherProject51;
      transientIMyGenericService1 = localInstance_1182D12741.GetMyService<int>();
      return new Program(transientIMyGenericService1);
    }
  }
}
```


