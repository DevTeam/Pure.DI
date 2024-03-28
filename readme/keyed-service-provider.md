#### Keyed service provider

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/KeyedServiceProviderScenario.cs)

```c#
interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service([Tag("Dependency Key")] IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition: IKeyedServiceProvider
{
    void Setup() =>
        DI.Setup(nameof(Composition))
            // The following hint overrides the name of the
            // "object Resolve(Type type)" method in "GetService",
            // which implements the "IServiceProvider" interface
            .Hint(Hint.ObjectResolveMethodName, "GetService")
            // The following hint overrides the name of the
            // "object Resolve(Type type, object tag)" method in "GetRequiredKeyedService",
            // which implements the "IKeyedServiceProvider" interface
            .Hint(Hint.ObjectResolveByTagMethodName, "GetRequiredKeyedService")
            .Bind<IDependency>("Dependency Key").As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>("Service Key").To<Service>()
            .Root<IDependency>(tag: "Dependency Key")
            .Root<IService>(tag: "Service Key");

    public object GetKeyedService(Type serviceType, object? serviceKey) =>
        GetRequiredKeyedService(serviceType, serviceKey);
}

var serviceProvider = new Composition();
var service = serviceProvider.GetRequiredKeyedService<IService>("Service Key");
var dependency = serviceProvider.GetRequiredKeyedService<IDependency>("Dependency Key");
service.Dependency.ShouldBe(dependency);
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    -IDependency _
    -IService _
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object GetService(Type type)
    + object GetRequiredKeyedService(Type type, object? tag)
  }
  Dependency --|> IDependency : "Dependency Key" 
  class Dependency {
    +Dependency()
  }
  Service --|> IService : "Service Key" 
  class Service {
    +Service(IDependency dependency)
  }
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  Service o--  "Singleton" Dependency : "Dependency Key"  IDependency
  Composition ..> Dependency : IDependency _
  Composition ..> Service : IService _
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition
{
  private readonly Composition _rootM03D28di;
  private readonly object _lockM03D28di;
  private Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Dependency _singletonM03D28di36_Dependency;
  
  public Composition()
  {
    _rootM03D28di = this;
    _lockM03D28di = new object();
  }
  
  internal Composition(Composition baseComposition)
  {
    _rootM03D28di = baseComposition._rootM03D28di;
    _lockM03D28di = _rootM03D28di._lockM03D28di;
  }
  
  public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency RootM03D28di0001
  {
    get
    {
      if (ReferenceEquals(_rootM03D28di._singletonM03D28di36_Dependency, null))
      {
          lock (_lockM03D28di)
          {
              if (ReferenceEquals(_rootM03D28di._singletonM03D28di36_Dependency, null))
              {
                  _singletonM03D28di36_Dependency = new Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Dependency();
                  _rootM03D28di._singletonM03D28di36_Dependency = _singletonM03D28di36_Dependency;
              }
          }
      }
      return _rootM03D28di._singletonM03D28di36_Dependency;
    }
  }
  
  public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService RootM03D28di0002
  {
    get
    {
      if (ReferenceEquals(_rootM03D28di._singletonM03D28di36_Dependency, null))
      {
          lock (_lockM03D28di)
          {
              if (ReferenceEquals(_rootM03D28di._singletonM03D28di36_Dependency, null))
              {
                  _singletonM03D28di36_Dependency = new Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Dependency();
                  _rootM03D28di._singletonM03D28di36_Dependency = _singletonM03D28di36_Dependency;
              }
          }
      }
      return new Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.Service(_rootM03D28di._singletonM03D28di36_Dependency);
    }
  }
  
  public T Resolve<T>()
  {
    return ResolverM03D28di<T>.Value.Resolve(this);
  }
  
  public T Resolve<T>(object? tag)
  {
    return ResolverM03D28di<T>.Value.ResolveByTag(this, tag);
  }
  
  public object GetService(global::System.Type type)
  {
    var index = (int)(_bucketSizeM03D28di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM03D28di;
    do {
      ref var pair = ref _bucketsM03D28di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  public object GetRequiredKeyedService(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM03D28di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM03D28di;
    do {
      ref var pair = ref _bucketsM03D28di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    -IDependency _\n" +
          "    -IService _\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object GetService(Type type)\n" +
          "    + object GetRequiredKeyedService(Type type, object? tag)\n" +
        "  }\n" +
        "  Dependency --|> IDependency : \"Dependency Key\" \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  Service --|> IService : \"Service Key\" \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Service o--  \"Singleton\" Dependency : \"Dependency Key\"  IDependency\n" +
        "  Composition ..> Dependency : IDependency _\n" +
        "  Composition ..> Service : IService _";
  }
  
  private readonly static int _bucketSizeM03D28di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[] _bucketsM03D28di;
  
  static Composition()
  {
    var valResolverM03D28di_0000 = new ResolverM03D28di_0000();
    ResolverM03D28di<Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency>.Value = valResolverM03D28di_0000;
    var valResolverM03D28di_0001 = new ResolverM03D28di_0001();
    ResolverM03D28di<Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService>.Value = valResolverM03D28di_0001;
    _bucketsM03D28di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<Composition, object>>.Create(
      4,
      out _bucketSizeM03D28di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency), valResolverM03D28di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<Composition, object>>(typeof(Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService), valResolverM03D28di_0001)
      });
  }
  
  private sealed class ResolverM03D28di<T>: global::Pure.DI.IResolver<Composition, T>
  {
    public static global::Pure.DI.IResolver<Composition, T> Value = new ResolverM03D28di<T>();
    
    public T Resolve(Composition composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(Composition composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM03D28di_0000: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency>
  {
    public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency Resolve(Composition composition)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency.");
    }
    
    public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case "Dependency Key":
          return composition.RootM03D28di0001;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IDependency.");
    }
  }
  
  private sealed class ResolverM03D28di_0001: global::Pure.DI.IResolver<Composition, Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService>
  {
    public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService Resolve(Composition composition)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService.");
    }
    
    public Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService ResolveByTag(Composition composition, object tag)
    {
      switch (tag)
      {
        case "Service Key":
          return composition.RootM03D28di0002;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.BCL.KeyedServiceProviderScenario.IService.");
    }
  }
}
```

</blockquote></details>

