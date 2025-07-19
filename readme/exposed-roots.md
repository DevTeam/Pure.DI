#### Exposed roots

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
using OtherAssembly;

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
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif

  private OtherAssembly.CompositionInOtherProject? _singleCompositionInOtherProject52;

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
    _lock = parentScope._lock;
  }

  public Program Program
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      OtherAssembly.IMyService transIMyService1;
      if (_root._singleCompositionInOtherProject52 is null)
      {
        lock (_lock)
        {
          _root._singleCompositionInOtherProject52 = new OtherAssembly.CompositionInOtherProject();
        }
      }

      OtherAssembly.CompositionInOtherProject localInstance_1182D12747 = _root._singleCompositionInOtherProject52;
      transIMyService1 = localInstance_1182D12747.MyService;
      return new Program(transIMyService1);
    }
  }
}
```


