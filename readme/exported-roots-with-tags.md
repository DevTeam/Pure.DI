#### Exported roots with tags

Composition roots from other assemblies or projects can be used as a source of bindings. When you add a binding to a composition from another assembly or project, the roots of the composition with the `RootKind.Exported` type will be used in the bindings automatically. For example, in some assembly a composition is defined as:
```c#
public partial class CompositionWithTagsInOtherProject
{
    private static void Setup() =>
        DI.Setup()
            .Bind().As(Lifetime.Singleton).To<MyDependency>()
            .Bind("Some tag").To<MyService>()
            .Root<IMyService>("MyService", "Some tag", RootKinds.Exported);
}
```
Use this when a library exposes ready-made composition roots that must be reused in another composition.


```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using OtherAssembly;

DI.Setup(nameof(Composition))
    // Binds to exposed composition roots from other project
    .Bind().As(Singleton).To<CompositionWithTagsInOtherProject>()
    .Root<Program>("Program");

var composition = new Composition();
var program = composition.Program;
program.DoSomething();

partial class Program([Tag("Some tag")] IMyService myService)
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

>[!WARNING]
>A tag on an exported root becomes the tag of the injected binding, so the consuming side must request the same tag to receive it instead of its own default binding.
>[!IMPORTANT]
>At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

Reach for it when a library exports more than one implementation of the same contract: the tag is what keeps the exported registrations apart on the consuming side.
Limitations: Exported roots create an integration contract between assemblies; tag names and root contracts should be versioned carefully.
Keep tags as constants shared by both assemblies, because a renamed string tag turns the import into a silent fallback to another binding.
See also: [Tags](tags.md), [Exported roots](exported-roots.md).

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

  private OtherAssembly.CompositionWithTagsInOtherProject? _singletonWcfLikeChannel;

  public Program Program
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      OtherAssembly.IMyService transientIMyService;
      if (_singletonWcfLikeChannel is null)
        lock (_lock)
          if (_singletonWcfLikeChannel is null)
          {
            _singletonWcfLikeChannel = new OtherAssembly.CompositionWithTagsInOtherProject();
          }

      OtherAssembly.CompositionWithTagsInOtherProject localInstance_1182D127 = _singletonWcfLikeChannel;
      transientIMyService = localInstance_1182D127.MyService;
      return new Program(transientIMyService);
    }
  }
}
```

</details>


See also:

- [Exported roots](exported-roots.md)

