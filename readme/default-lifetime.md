#### Default lifetime

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Lifetimes/DefaultLifetimeScenario.cs)

For example, if some lifetime is used more often than others, you can make it the default lifetime:

```c#
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService
{
    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

internal class Service : IService
{
    public Service(IDependency dependency1, IDependency dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }

    public IDependency Dependency1 { get; }

    public IDependency Dependency2 { get; }
}

DI.Setup("Composition")
    .DefaultLifetime(Lifetime.Singleton)
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>().Root<IService>("Root");

var composition = new Composition();
var service1 = composition.Root;
var service2 = composition.Root;
service1.ShouldBe(service2);
service1.Dependency1.ShouldBe(service1.Dependency2);
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +IService Root
    +T ResolveᐸTᐳ()
    +T ResolveᐸTᐳ(object? tag)
    +object ResolveᐸTᐳ(Type type)
    +object ResolveᐸTᐳ(Type type, object? tag)
  }
  Service --|> IService : 
  class Service {
    +Service(IDependency dependency1, IDependency dependency2)
  }
  Dependency --|> IDependency : 
  class Dependency {
    +Dependency()
  }
  class IService {
    <<abstract>>
  }
  class IDependency {
    <<abstract>>
  }
  Service o--  "Singleton" Dependency : IDependency dependency1
  Service o--  "Singleton" Dependency : IDependency dependency2
  Composition ..> Service : IService Root
```

</details>

<details>
<summary>Generated Code</summary>

```c#
partial class Composition
{
  private readonly System.IDisposable[] _disposables523744;
  private Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.Dependency _f22Singleton523744;
  private Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.Service _f23Singleton523744;
  
  public Composition()
  {
    _disposables523744 = new System.IDisposable[0];
  }
  
  internal Composition(Composition parent)
  {
    _disposables523744 = new System.IDisposable[0];
    lock (parent._disposables523744)
    {
      _f22Singleton523744 = parent._f22Singleton523744;
      _f23Singleton523744 = parent._f23Singleton523744;
    }
  }
  
  #region Composition Roots
  public Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.IService Root
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    get
    {
      if (global::System.Object.ReferenceEquals(_f22Singleton523744, null))
      {
          lock (_disposables523744)
          {
              if (global::System.Object.ReferenceEquals(_f22Singleton523744, null))
              {
                  _f22Singleton523744 = new Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.Dependency();
              }
          }
      }
      if (global::System.Object.ReferenceEquals(_f23Singleton523744, null))
      {
          lock (_disposables523744)
          {
              if (global::System.Object.ReferenceEquals(_f23Singleton523744, null))
              {
                  _f23Singleton523744 = new Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.Service(_f22Singleton523744, _f22Singleton523744);
              }
          }
      }
      return _f23Singleton523744;
    }
  }
  #endregion
  
  #region API
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public T Resolve<T>()
  {
    return Resolver523744<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public T Resolve<T>(object? tag)
  {
    return Resolver523744<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public object Resolve(global::System.Type type)
  {
    int index = (int)(_bucketSize523744 * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _buckets523744[index];
    if (ReferenceEquals(pair.Key, type))
    {
      return pair.Value.Resolve(this);
    }
    
    int maxIndex = index + _bucketSize523744;
    for (int i = index + 1; i < maxIndex; i++)
    {
      pair = ref _buckets523744[i];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    }
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
  public object Resolve(global::System.Type type, object? tag)
  {
    int index = (int)(_bucketSize523744 * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _buckets523744[index];
    if (ReferenceEquals(pair.Key, type))
    {
      return pair.Value.ResolveByTag(this, tag);
    }
    
    int maxIndex = index + _bucketSize523744;
    for (int i = index + 1; i < maxIndex; i++)
    {
      pair = ref _buckets523744[i];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  #endregion
  
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +IService Root\n" +
          "    +T ResolveᐸTᐳ()\n" +
          "    +T ResolveᐸTᐳ(object? tag)\n" +
          "    +object ResolveᐸTᐳ(Type type)\n" +
          "    +object ResolveᐸTᐳ(Type type, object? tag)\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency1, IDependency dependency2)\n" +
        "  }\n" +
        "  Dependency --|> IDependency : \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Service o--  \"Singleton\" Dependency : IDependency dependency1\n" +
        "  Service o--  \"Singleton\" Dependency : IDependency dependency2\n" +
        "  Composition ..> Service : IService Root";
  }
  
  private readonly static int _bucketSize523744;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _buckets523744;
  
  static Composition()
  {
    Resolver5237440 valResolver5237440 = new Resolver5237440();
    Resolver523744<Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.IService>.Value = valResolver5237440;
    _buckets523744 = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      1,
      out _bucketSize523744,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[1]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.IService), valResolver5237440)
      });
  }
  
  #region Resolvers
  private class Resolver523744<T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value;
  }
  
  private sealed class Resolver5237440: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.IService>
  {
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    public Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.IService Resolve(Composition composition)
    {
      return composition.Root;
    }
    
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x300)]
    public Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.IService ResolveByTag(Composition composition, object tag)
    {
      if (Equals(tag, null)) return composition.Root;
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario.IService.");
    }
  }
  #endregion
}
```

</details>
