#### Custom dispose strategy for Owned<T>

`Owned<T>` calls `Dispose()` on every tracked resource when the owner is disposed. Some types — most notably WCF clients that implement `ICommunicationObject` — require a specific shutdown sequence (try `Close()`, fall back to `Abort()` on a timeout or when the channel is faulted). Implement the partial `OnDispose` / `OnDisposeAsync` hooks on the non-generic `Pure.DI.Owned` accumulator to perform the custom shutdown logic and set `skipDefaultDispose` to `true` to skip the default disposal call (or leave it `false` to let the owner dispose the instance as usual).


```c#
using Shouldly;
using Pure.DI;

// The root is Owned<IWcfClient>, so the resources it tracks are released
// when the Owned<T> owner is disposed, not when the composition is.
var composition = new Composition();
var ownedClient = composition.Client;

// Disposing the owner runs the custom hook instead of the default disposal.
ownedClient.Dispose();

// Only "Close" was recorded: the hook performed the graceful shutdown and
// skipped the default Dispose(), which would have recorded "Default".
ownedClient.Value.ShutdownSequence.ShouldBe(["Close"]);

DI.Setup(nameof(Composition))
    .Bind<IWcfClient>().To<WcfClient>()
    .Root<Owned<IWcfClient>>("Client");

    // Minimal stand-in for a WCF client (ICommunicationObject): a graceful
    // Close(timeout) plus an Abort() fallback.
    public interface IWcfClient
    {
        // Records the shutdown calls in order so the test can assert which path was taken.
        IReadOnlyList<string> ShutdownSequence { get; }

        void Close(TimeSpan timeout);

        void Abort();
    }

    public sealed class WcfClient : IWcfClient, IDisposable
    {
        private readonly List<string> _sequence = [];

        public IReadOnlyList<string> ShutdownSequence => _sequence;

        public void Close(TimeSpan timeout) => _sequence.Add("Close");

        public void Abort() => _sequence.Add("Abort");

        // Only reached when the owner falls back to the default disposal strategy.
        public void Dispose() => _sequence.Add("Default");
    }
}

// The hook belongs to the non-generic Pure.DI.Owned accumulator shared by every Owned<T>,
// so the partial implementation must be declared in the Pure.DI namespace.
namespace Pure.DI
{
    internal sealed partial class Owned
    {
        // Called for each IDisposable tracked by any Owned<T> graph before the default disposal.
        // Set skipDefaultDispose to true after releasing the instance yourself, otherwise the
        // accumulator disposes it in the usual way.
        partial void OnDispose<T>(in T disposableInstance, ref bool skipDefaultDispose)
            where T : IDisposable
        {
            switch (disposableInstance)
            {
                // WCF-like clients need Close(timeout) with an Abort() fallback instead of Dispose().
                case UsageTests.Advanced.CustomOwnedDisposeStrategyScenario.IWcfClient wcfClient:
                    wcfClient.Close(TimeSpan.FromSeconds(10));
                    skipDefaultDispose = true;
                    break;

                // Any other resource: leave skipDefaultDispose as it is and let the
                // accumulator dispose it as usual.
            }
        }
    }
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
>The hook belongs to the non-generic `Pure.DI.Owned` accumulator used internally by every `Owned<T>`, so its partial implementation must be declared in the `Pure.DI` namespace.

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

  public Owned<IWcfClient> Client
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var perBlockOwned = new Owned(1, _lock);
      try
      {
        Owned<IWcfClient> perBlockOwnedIWcfClient;
        // Tracks owned disposables
        Owned transientOwned;
        Owned localOwned1 = perBlockOwned;
        transientOwned = localOwned1;
        IOwned localOwned = transientOwned;
        // Creates the owned value
        var transientWcfClient = new WcfClient();
        lock (_lock)
        {
          perBlockOwned.Add(transientWcfClient);
        }

        IWcfClient localValue = transientWcfClient;
        perBlockOwnedIWcfClient = new Owned<IWcfClient>(localValue, localOwned);
        lock (_lock)
        {
          perBlockOwned.Add(perBlockOwnedIWcfClient);
        }

        return perBlockOwnedIWcfClient;
      }
      catch
      {
        if (!Object.ReferenceEquals(perBlockOwned, null))
        {
          try
          {
            ((IDisposable)perBlockOwned).Dispose();
          }
          catch
          {
          // Preserve the original graph construction exception.
          }
        }

        throw;
      }
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
    #if NETCOREAPP3_0_OR_GREATER
    var index = (int)(_bucketSize * (((uint)type.TypeHandle.GetHashCode()) % 1));
    #else
    var index = (int)(_bucketSize * (((uint)RuntimeHelpers.GetHashCode(type)) % 1));
    #endif
    ref var pair = ref _buckets[index];
    return Object.ReferenceEquals(pair.Key, type) ? pair.Value.Resolve(this) : Resolve(type, index);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private object Resolve(Type type, int index)
  {
    var finish = index + _bucketSize;
    while (++index < finish)
    {
      ref var pair = ref _buckets[index];
      if (Object.ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    }

    throw new CannotResolveException($"{CannotResolveMessage} {OfTypeMessage} {type}.", type, null);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object Resolve(Type type, object? tag)
  {
    #if NETCOREAPP3_0_OR_GREATER
    var index = (int)(_bucketSize * (((uint)type.TypeHandle.GetHashCode()) % 1));
    #else
    var index = (int)(_bucketSize * (((uint)RuntimeHelpers.GetHashCode(type)) % 1));
    #endif
    ref var pair = ref _buckets[index];
    return Object.ReferenceEquals(pair.Key, type) ? pair.Value.ResolveByTag(this, tag) : Resolve(type, tag, index);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private object Resolve(Type type, object? tag, int index)
  {
    var finish = index + _bucketSize;
    while (++index < finish)
    {
      ref var pair = ref _buckets[index];
      if (Object.ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }

    throw new CannotResolveException($"{CannotResolveMessage} \"{tag}\" {OfTypeMessage} {type}.", type, tag);
  }

  private readonly static uint _bucketSize;
  private readonly static Pair<IResolver<Composition, object>>[] _buckets;

  static Composition()
  {
    var valResolver_0000 = new Resolver_0000();
    Resolver<Owned<IWcfClient>>.Value = valResolver_0000;
    _buckets = Buckets<IResolver<Composition, object>>.Create(
      1,
      out _bucketSize,
      new Pair<IResolver<Composition, object>>[1]
      {
         new Pair<IResolver<Composition, object>>(typeof(Owned<IWcfClient>), valResolver_0000)
      });
  }

  private const string CannotResolveMessage = "Cannot resolve composition root ";
  private const string OfTypeMessage = "of type ";

  private class Resolver<T>: IResolver<Composition, T>
  {
    public static IResolver<Composition, T> Value = new Resolver<T>();

    public virtual T Resolve(Composition composite)
    {
      throw new CannotResolveException($"{CannotResolveMessage}{OfTypeMessage}{typeof(T)}.", typeof(T), null);
    }

    public virtual T ResolveByTag(Composition composite, object tag)
    {
      throw new CannotResolveException($"{CannotResolveMessage}\"{tag}\" {OfTypeMessage}{typeof(T)}.", typeof(T), tag);
    }
  }

  private sealed class Resolver_0000: Resolver<Owned<IWcfClient>>, IResolver<Composition, object>
  {
    public override Owned<IWcfClient> Resolve(Composition composition)
    {
      return composition.Client;
    }

    public override Owned<IWcfClient> ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Client;

        default:
          return base.ResolveByTag(composition, tag);
      }
    }
    object IResolver<Composition, object>.Resolve(Composition composition)
    {
      return Resolve(composition);
    }

    object IResolver<Composition, object>.ResolveByTag(Composition composition, object tag)
    {
      return ResolveByTag(composition, tag);
    }
  }
}
```

</details>


See also:

- [Tracing exceptions during Owned disposal](tracing-exceptions-during-owned-disposal.md)
- [Custom dispose strategy](custom-dispose-strategy.md)

