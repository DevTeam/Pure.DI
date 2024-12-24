#### Exposed roots

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/ExposedRootsScenario.cs)

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
- Create a net9.0 (or later) console application
- Add reference to NuGet package
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
- Copy the example code into the _Program.cs_ file

You are ready to run the example!

</details>

> [!IMPORTANT]
> At this point, a composition from another assembly or another project can be used for this purpose. Compositions from the current project cannot be used in this way due to limitations of the source code generators.

The following partial class will be generated:

```c#
partial class Composition
{
  private readonly Composition _root;
  private readonly Lock _lock;

  private OtherAssembly.CompositionInOtherProject? _singletonCompositionInOtherProject43;

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
      if (_root._singletonCompositionInOtherProject43 is null)
      {
        using (_lock.EnterScope())
        {
          if (_root._singletonCompositionInOtherProject43 is null)
          {
            _root._singletonCompositionInOtherProject43 = new OtherAssembly.CompositionInOtherProject();
          }
        }
      }

      OtherAssembly.IMyService transientIMyService1;
      OtherAssembly.CompositionInOtherProject localInstance_1182D1275 = _root._singletonCompositionInOtherProject43;
      transientIMyService1 = localInstance_1182D1275.MyService;
      return new Program(transientIMyService1);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T Resolve<T>()
  {
    return Resolver<T>.Value.Resolve(this);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T Resolve<T>(object? tag)
  {
    return Resolver<T>.Value.ResolveByTag(this, tag);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type)
  {
    var index = (int)(_bucketSize * ((uint)RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _buckets[index];
    return pair.Key == type ? pair.Value.Resolve(this) : Resolve(type, index);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private object Resolve(Type type, int index)
  {
    var finish = index + _bucketSize;
    while (++index < finish)
    {
      ref var pair = ref _buckets[index];
      if (pair.Key == type)
      {
        return pair.Value.Resolve(this);
      }
    }

    throw new InvalidOperationException($"{CannotResolveMessage} {OfTypeMessage} {type}.");
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type, object? tag)
  {
    var index = (int)(_bucketSize * ((uint)RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _buckets[index];
    return pair.Key == type ? pair.Value.ResolveByTag(this, tag) : Resolve(type, tag, index);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private object Resolve(Type type, object? tag, int index)
  {
    var finish = index + _bucketSize;
    while (++index < finish)
    {
      ref var pair = ref _buckets[index];
      if (pair.Key == type)
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }

    throw new InvalidOperationException($"{CannotResolveMessage} \"{tag}\" {OfTypeMessage} {type}.");
  }

  private readonly static int _bucketSize;
  private readonly static Pair<Type, IResolver<Composition, object>>[] _buckets;

  static Composition()
  {
    var valResolver_0000 = new Resolver_0000();
    Resolver<Program>.Value = valResolver_0000;
    _buckets = Buckets<Type, IResolver<Composition, object>>.Create(
      1,
      out _bucketSize,
      new Pair<Type, IResolver<Composition, object>>[1]
      {
         new Pair<Type, IResolver<Composition, object>>(typeof(Program), valResolver_0000)
      });
  }

  private const string CannotResolveMessage = "Cannot resolve composition root ";
  private const string OfTypeMessage = "of type ";

  private class Resolver<T>: IResolver<Composition, T>
  {
    public static IResolver<Composition, T> Value = new Resolver<T>();

    public virtual T Resolve(Composition composite)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}{OfTypeMessage}{typeof(T)}.");
    }

    public virtual T ResolveByTag(Composition composite, object tag)
    {
      throw new InvalidOperationException($"{CannotResolveMessage}\"{tag}\" {OfTypeMessage}{typeof(T)}.");
    }
  }

  private sealed class Resolver_0000: Resolver<Program>
  {
    public override Program Resolve(Composition composition)
    {
      return composition.Program;
    }

    public override Program ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Program;

        default:
          return base.ResolveByTag(composition, tag);
      }
    }
  }
}
```


