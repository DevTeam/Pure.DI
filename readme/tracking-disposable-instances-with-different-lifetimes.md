#### Tracking disposable instances with different lifetimes

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Advanced/TrackingDisposableWithDifferentLifetimesScenario.cs)


```c#
using Shouldly;
using Pure.DI;

var composition = new Composition();
var root1 = composition.Root;
var root2 = composition.Root;
root1.Dependency.ShouldNotBe(root2.Dependency);
root1.SingleDependency.ShouldBe(root2.SingleDependency);

root2.Dispose();

// Checks that the disposable instances
// associated with root1 have been disposed of
root2.Dependency.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
root2.SingleDependency.IsDisposed.ShouldBeFalse();

// Checks that the disposable instances
// associated with root2 have not been disposed of
root1.Dependency.IsDisposed.ShouldBeFalse();
root1.SingleDependency.IsDisposed.ShouldBeFalse();

root1.Dispose();

// Checks that the disposable instances
// associated with root2 have been disposed of
root1.Dependency.IsDisposed.ShouldBeTrue();

// But the singleton is still not disposed of
root1.SingleDependency.IsDisposed.ShouldBeFalse();
        
composition.Dispose();
root1.SingleDependency.IsDisposed.ShouldBeTrue();

interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }

    public IDependency SingleDependency { get; }
}

class Service(
    Func<Owned<IDependency>> dependencyFactory,
    [Tag("single")] Func<Owned<IDependency>> singleDependencyFactory)
    : IService, IDisposable
{
    private readonly Owned<IDependency> _dependency = dependencyFactory();
    private readonly Owned<IDependency> _singleDependency = singleDependencyFactory();

    public IDependency Dependency => _dependency.Value;

    public IDependency SingleDependency => _singleDependency.Value;

    public void Dispose()
    {
        _dependency.Dispose();
        _singleDependency.Dispose();
    }
}

partial class Composition
{
    static void Setup() =>
        DI.Setup()
            .Bind().To<Dependency>()
            .Bind("single").As(Lifetime.Singleton).To<Dependency>()
            .Bind().To<Service>()

            // Composition root
            .Root<Service>("Root");
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
- Add references to NuGet packages
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

The following partial class will be generated:

```c#
partial class Composition: IDisposable
{
  private readonly Composition _root;
  private readonly Lock _lock;
  private object[] _disposables;
  private int _disposeIndex;

  private Dependency? _singletonDependency44;

  [OrdinalAttribute(256)]
  public Composition()
  {
    _root = this;
    _lock = new Lock();
    _disposables = new object[1];
  }

  internal Composition(Composition parentScope)
  {
    _root = (parentScope ?? throw new ArgumentNullException(nameof(parentScope)))._root;
    _lock = _root._lock;
    _disposables = parentScope._disposables;
  }

  public Service Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var accumulator50 = new Owned();
      Func<Owned<IDependency>> perBlockFunc2 = new Func<Owned<IDependency>>([MethodImpl(MethodImplOptions.AggressiveInlining)] () =>
      {
        var accumulator50 = new Owned();
        if (_root._singletonDependency44 is null)
        {
          using (_lock.EnterScope())
          {
            if (_root._singletonDependency44 is null)
            {
              _root._singletonDependency44 = new Dependency();
              _root._disposables[_root._disposeIndex++] = _root._singletonDependency44;
            }
          }
        }

        Owned transientOwned4;
        Owned localOwned30 = accumulator50;
        transientOwned4 = localOwned30;
        using (_lock.EnterScope())
        {
          accumulator50.Add(transientOwned4);
        }
        Owned<IDependency> perBlockOwned3;
        // Creates the owner of an instance
        IOwned localOwned31 = transientOwned4;
        IDependency localValue32 = _root._singletonDependency44;
        perBlockOwned3 = new Owned<IDependency>(localValue32, localOwned31);
        using (_lock.EnterScope())
        {
          accumulator50.Add(perBlockOwned3);
        }
        Owned<IDependency> localValue29 = perBlockOwned3;
        return localValue29;
      });
      Func<Owned<IDependency>> perBlockFunc1 = new Func<Owned<IDependency>>([MethodImpl(MethodImplOptions.AggressiveInlining)] () =>
      {
        var accumulator50 = new Owned();
        Dependency transientDependency8 = new Dependency();
        using (_lock.EnterScope())
        {
          accumulator50.Add(transientDependency8);
        }
        Owned transientOwned7;
        Owned localOwned34 = accumulator50;
        transientOwned7 = localOwned34;
        using (_lock.EnterScope())
        {
          accumulator50.Add(transientOwned7);
        }
        Owned<IDependency> perBlockOwned6;
        // Creates the owner of an instance
        IOwned localOwned35 = transientOwned7;
        IDependency localValue36 = transientDependency8;
        perBlockOwned6 = new Owned<IDependency>(localValue36, localOwned35);
        using (_lock.EnterScope())
        {
          accumulator50.Add(perBlockOwned6);
        }
        Owned<IDependency> localValue33 = perBlockOwned6;
        return localValue33;
      });
      Service transientService0 = new Service(perBlockFunc1, perBlockFunc2);
      using (_lock.EnterScope())
      {
        accumulator50.Add(transientService0);
      }
      return transientService0;
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

  public void Dispose()
  {
    int disposeIndex;
    object[] disposables;
    using (_lock.EnterScope())
    {
      disposeIndex = _disposeIndex;
      _disposeIndex = 0;
      disposables = _disposables;
      _disposables = new object[1];
      _singletonDependency44 = null;
      }

      while (disposeIndex-- > 0)
      {
        switch (disposables[disposeIndex])
        {
          case IDisposable disposableInstance:
            try
            {
              disposableInstance.Dispose();
            }
            catch (Exception exception)
            {
              OnDisposeException(disposableInstance, exception);
            }
            break;
        }
      }
    }

    partial void OnDisposeException<T>(T disposableInstance, Exception exception) where T : IDisposable;

    private readonly static int _bucketSize;
    private readonly static Pair<Type, IResolver<Composition, object>>[] _buckets;

    static Composition()
    {
      var valResolver_0000 = new Resolver_0000();
      Resolver<Service>.Value = valResolver_0000;
      _buckets = Buckets<Type, IResolver<Composition, object>>.Create(
        1,
        out _bucketSize,
        new Pair<Type, IResolver<Composition, object>>[1]
        {
           new Pair<Type, IResolver<Composition, object>>(typeof(Service), valResolver_0000)
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

    private sealed class Resolver_0000: Resolver<Service>
    {
      public override Service Resolve(Composition composition)
      {
        return composition.Root;
      }

      public override Service ResolveByTag(Composition composition, object tag)
      {
        switch (tag)
        {
          case null:
            return composition.Root;

          default:
            return base.ResolveByTag(composition, tag);
        }
      }
    }
}
```

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Composition --|> IDisposable
	Owned --|> IOwned
	Dependency --|> IDependency
	Composition ..> Service : Service Root
	Service o-- "PerBlock" FuncᐸOwnedᐸIDependencyᐳᐳ : FuncᐸOwnedᐸIDependencyᐳᐳ
	Service o-- "PerBlock" FuncᐸOwnedᐸIDependencyᐳᐳ : "single"  FuncᐸOwnedᐸIDependencyᐳᐳ
	FuncᐸOwnedᐸIDependencyᐳᐳ o-- "PerBlock" OwnedᐸIDependencyᐳ : OwnedᐸIDependencyᐳ
	FuncᐸOwnedᐸIDependencyᐳᐳ o-- "PerBlock" OwnedᐸIDependencyᐳ : "single"  OwnedᐸIDependencyᐳ
	OwnedᐸIDependencyᐳ *--  Owned : IOwned
	OwnedᐸIDependencyᐳ *--  Dependency : IDependency
	OwnedᐸIDependencyᐳ *--  Owned : IOwned
	OwnedᐸIDependencyᐳ o-- "Singleton" Dependency : "single"  IDependency
	namespace Pure.DI {
		class IOwned {
			<<interface>>
		}
		class Owned {
		}
		class OwnedᐸIDependencyᐳ {
				<<struct>>
		}
	}
	namespace Pure.DI.UsageTests.Advanced.TrackingDisposableWithDifferentLifetimesScenario {
		class Composition {
		<<partial>>
		+Service Root
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
		}
		class Dependency {
			+Dependency()
		}
		class IDependency {
			<<interface>>
		}
		class Service {
		}
	}
	namespace System {
		class FuncᐸOwnedᐸIDependencyᐳᐳ {
				<<delegate>>
		}
		class IDisposable {
			<<abstract>>
		}
	}
```

