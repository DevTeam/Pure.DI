#### Service provider

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/ServiceProviderScenario.cs)

The `// ObjectResolveMethodName = GetService` hint overrides the _object Resolve(Type type)_ method name in _GetService_, allowing the _IServiceProvider_ interface to be implemented in a partial class.

```c#
interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class ServiceProvider: IServiceProvider
{
    private void Setup() =>
        // The following hint overrides the name of the
        // "object Resolve(Type type)" method in "GetService",
        // which implements the "IServiceProvider" interface:

        // ObjectResolveMethodName = GetService
        DI.Setup(nameof(ServiceProvider))
            .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IDependency>()
            .Root<IService>();
}

var serviceProvider = new ServiceProvider();
var service = (IService)serviceProvider.GetService(typeof(IService));
var dependency = serviceProvider.GetService(typeof(IDependency));
service.Dependency.ShouldBe(dependency);
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class ServiceProvider {
    -IDependency RootM02D01di0001
    -IService RootM02D01di0002
    + T ResolveᐸTᐳ()
    + T ResolveᐸTᐳ(object? tag)
    + object GetService(Type type)
    + object Resolve(Type type, object? tag)
  }
  Dependency --|> IDependency : 
  class Dependency {
    +Dependency()
  }
  Service --|> IService : 
  class Service {
    +Service(IDependency dependency)
  }
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  Service o--  "Singleton" Dependency : IDependency
  ServiceProvider ..> Dependency : IDependency RootM02D01di0001
  ServiceProvider ..> Service : IService RootM02D01di0002
```

</details>

<details>
<summary>Pure.DI-generated partial class ServiceProvider</summary><blockquote>

```c#
partial class ServiceProvider
{
  private readonly global::System.IDisposable[] _disposableSingletonsM02D01di;
  private Pure.DI.UsageTests.BCL.ServiceProviderScenario.Dependency _singletonM02D01di34_Dependency;
  
  public ServiceProvider()
  {
    _disposableSingletonsM02D01di = new global::System.IDisposable[0];
  }
  
  internal ServiceProvider(ServiceProvider parent)
  {
    _disposableSingletonsM02D01di = new global::System.IDisposable[0];
    lock (parent._disposableSingletonsM02D01di)
    {
      _singletonM02D01di34_Dependency = parent._singletonM02D01di34_Dependency;
    }
  }
  
  #region Composition Roots
  public Pure.DI.UsageTests.BCL.ServiceProviderScenario.IDependency RootM02D01di0001
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      if (object.ReferenceEquals(_singletonM02D01di34_Dependency, null))
      {
          lock (_disposableSingletonsM02D01di)
          {
              if (object.ReferenceEquals(_singletonM02D01di34_Dependency, null))
              {
                  _singletonM02D01di34_Dependency = new Pure.DI.UsageTests.BCL.ServiceProviderScenario.Dependency();
              }
          }
      }
      return _singletonM02D01di34_Dependency;
    }
  }
  
  public Pure.DI.UsageTests.BCL.ServiceProviderScenario.IService RootM02D01di0002
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      if (object.ReferenceEquals(_singletonM02D01di34_Dependency, null))
      {
          lock (_disposableSingletonsM02D01di)
          {
              if (object.ReferenceEquals(_singletonM02D01di34_Dependency, null))
              {
                  _singletonM02D01di34_Dependency = new Pure.DI.UsageTests.BCL.ServiceProviderScenario.Dependency();
              }
          }
      }
      return new Pure.DI.UsageTests.BCL.ServiceProviderScenario.Service(_singletonM02D01di34_Dependency);
    }
  }
  #endregion
  
  #region API
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>()
  {
    return ResolverM02D01di<T>.Value.Resolve(this);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public T Resolve<T>(object? tag)
  {
    return ResolverM02D01di<T>.Value.ResolveByTag(this, tag);
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object GetService(global::System.Type type)
  {
    var index = (int)(_bucketSizeM02D01di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM02D01di;
    do {
      ref var pair = ref _bucketsM02D01di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.Resolve(this);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }
  
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public object Resolve(global::System.Type type, object? tag)
  {
    var index = (int)(_bucketSizeM02D01di * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % 4));
    var finish = index + _bucketSizeM02D01di;
    do {
      ref var pair = ref _bucketsM02D01di[index];
      if (ReferenceEquals(pair.Key, type))
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    } while (++index < finish);
    
    throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }
  #endregion
  
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class ServiceProvider {\n" +
          "    -IDependency RootM02D01di0001\n" +
          "    -IService RootM02D01di0002\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object GetService(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  Dependency --|> IDependency : \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(IDependency dependency)\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Service o--  \"Singleton\" Dependency : IDependency\n" +
        "  ServiceProvider ..> Dependency : IDependency RootM02D01di0001\n" +
        "  ServiceProvider ..> Service : IService RootM02D01di0002";
  }
  
  private readonly static int _bucketSizeM02D01di;
  private readonly static global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<ServiceProvider, object>>[] _bucketsM02D01di;
  
  static ServiceProvider()
  {
    var valResolverM02D01di_0000 = new ResolverM02D01di_0000();
    ResolverM02D01di<Pure.DI.UsageTests.BCL.ServiceProviderScenario.IDependency>.Value = valResolverM02D01di_0000;
    var valResolverM02D01di_0001 = new ResolverM02D01di_0001();
    ResolverM02D01di<Pure.DI.UsageTests.BCL.ServiceProviderScenario.IService>.Value = valResolverM02D01di_0001;
    _bucketsM02D01di = global::Pure.DI.Buckets<global::System.Type, global::Pure.DI.IResolver<ServiceProvider, object>>.Create(
      4,
      out _bucketSizeM02D01di,
      new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<ServiceProvider, object>>[2]
      {
         new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<ServiceProvider, object>>(typeof(Pure.DI.UsageTests.BCL.ServiceProviderScenario.IDependency), valResolverM02D01di_0000)
        ,new global::Pure.DI.Pair<global::System.Type, global::Pure.DI.IResolver<ServiceProvider, object>>(typeof(Pure.DI.UsageTests.BCL.ServiceProviderScenario.IService), valResolverM02D01di_0001)
      });
  }
  
  #region Resolvers
  private sealed class ResolverM02D01di<T>: global::Pure.DI.IResolver<ServiceProvider, T>
  {
    public static global::Pure.DI.IResolver<ServiceProvider, T> Value = new ResolverM02D01di<T>();
    
    public T Resolve(ServiceProvider composite)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }
    
    public T ResolveByTag(ServiceProvider composite, object tag)
    {
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }
  
  private sealed class ResolverM02D01di_0000: global::Pure.DI.IResolver<ServiceProvider, Pure.DI.UsageTests.BCL.ServiceProviderScenario.IDependency>
  {
    public Pure.DI.UsageTests.BCL.ServiceProviderScenario.IDependency Resolve(ServiceProvider composition)
    {
      return composition.RootM02D01di0001;
    }
    
    public Pure.DI.UsageTests.BCL.ServiceProviderScenario.IDependency ResolveByTag(ServiceProvider composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.RootM02D01di0001;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.BCL.ServiceProviderScenario.IDependency.");
    }
  }
  
  private sealed class ResolverM02D01di_0001: global::Pure.DI.IResolver<ServiceProvider, Pure.DI.UsageTests.BCL.ServiceProviderScenario.IService>
  {
    public Pure.DI.UsageTests.BCL.ServiceProviderScenario.IService Resolve(ServiceProvider composition)
    {
      return composition.RootM02D01di0002;
    }
    
    public Pure.DI.UsageTests.BCL.ServiceProviderScenario.IService ResolveByTag(ServiceProvider composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.RootM02D01di0002;
      }
      throw new global::System.InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type Pure.DI.UsageTests.BCL.ServiceProviderScenario.IService.");
    }
  }
  #endregion
}
```

</blockquote></details>

