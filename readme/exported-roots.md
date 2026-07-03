#### Exported roots

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind().To<MyService>()
            .Root<IMyService>("MyService", kind: RootKinds.Exported);
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

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add a reference to the NuGet package
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

>[!IMPORTANT]
>At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock = new Lock();
#else
  private readonly Object _lock = new Object();
#endif

  private OtherAssembly.CompositionInOtherProject? _singletonCompositionInOtherProject71;

  public Program Program
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      OtherAssembly.IMyService transientIMyService;
      if (_singletonCompositionInOtherProject71 is null)
        lock (_lock)
          if (_singletonCompositionInOtherProject71 is null)
          {
            _singletonCompositionInOtherProject71 = new OtherAssembly.CompositionInOtherProject();
          }

      OtherAssembly.CompositionInOtherProject localInstance_1182D127 = _singletonCompositionInOtherProject71;
      transientIMyService = localInstance_1182D127.MyService;
      return new Program(transientIMyService);
    }
  }
}
```

</details>


See also:

- [Exported roots with tags](exported-roots-with-tags.md)
- [Exported generic roots](exported-generic-roots.md)

