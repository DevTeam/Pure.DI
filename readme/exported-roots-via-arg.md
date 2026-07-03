#### Exported roots via arg

Composition roots from other assemblies or projects can be used as a source of bindings passed through composition arguments. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
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
using Shouldly;
using Pure.DI;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Arg<CompositionInOtherProject>("baseComposition")
    .Root<Program>("Program");

var baseComposition = new CompositionInOtherProject();
var composition = new Composition(baseComposition);
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
- Add references to the NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
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
  private readonly OtherAssembly.CompositionInOtherProject _argBaseComposition;

  [OrdinalAttribute(128)]
  public Composition(OtherAssembly.CompositionInOtherProject baseComposition)
  {
    _argBaseComposition = baseComposition ?? throw new ArgumentNullException(nameof(baseComposition));
  }

  public Program Program
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      OtherAssembly.IMyService transientIMyService;
      OtherAssembly.CompositionInOtherProject localInstance_1182D127 = _argBaseComposition;
      transientIMyService = localInstance_1182D127.MyService;
      return new Program(transientIMyService);
    }
  }
}
```

</details>


See also:

- [Exported roots](exported-roots.md)

