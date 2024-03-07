#### Root arguments

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/RootArgumentsScenario.cs)

Sometimes it is necessary to pass some state to the composition root to use it when resolving dependencies. To do this, just use the `RootArg<T>(string argName)` method, specify the type of argument and its name. You can also specify a tag for each argument. You can then use them as dependencies when building the object graph. If you have multiple arguments of the same type, just use tags to distinguish them. The root of a composition that uses at least one root argument is prepended as a method, not a property. It is important to remember that the method will only display arguments that are used in the object graph of that composition root. Arguments that are not involved will not be added to the method arguments. It is best to use unique argument names so that there are no collisions.

```c#
interface IDependency
{
    int Id { get; }

    public string DependencyName { get; }
}

class Dependency(int id, string dependencyName) : IDependency
{
    public int Id { get; } = id;

    public string DependencyName { get; } = dependencyName;
}

interface IService
{
    string Name { get; }

    IDependency Dependency { get; }
}

class Service(
    [Tag("forService")] string name,
    IDependency dependency)
    : IService
{
    public string Name { get; } = name;

    public IDependency Dependency { get; } = dependency;
}

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>().Root<IService>("CreateService")
    // Some argument
    .RootArg<int>("id")
    .RootArg<string>("dependencyName")
    // An argument can be tagged (e.g., tag "forService")
    // to be injectable by type and this tag
    .RootArg<string>("serviceName", "forService");

var composition = new Composition();
        
// service = new Service("Abc", new Dependency(123, "dependency 123"));
var service = composition.CreateService(serviceName: "Abc", id: 123, dependencyName: "dependency 123");
        
service.Name.ShouldBe("Abc");
service.Dependency.Id.ShouldBe(123);
service.Dependency.DependencyName.ShouldBe("dependency 123");
```

When using composition root arguments, compilation warnings are shown if `Resolve` methods are generated, since these methods will not be able to create these roots. You can disable the creation of `Resolve` methods using the `Hint(Hint.Resolve, "Off")` hint, or ignore them but remember the risks of using `Resolve` methods.

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +IService CreateService(int id, string dependencyName, string serviceName)
  }
  class Int32
  class String
  Dependency --|> IDependency : 
  class Dependency {
    +Dependency(Int32 id, String dependencyName)
  }
  Service --|> IService : 
  class Service {
    +Service(String name, IDependency dependency)
  }
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  Dependency o-- Int32 : Argument "id"
  Dependency o-- String : Argument "dependencyName"
  Service o-- String : "forService"  Argument "serviceName"
  Service *--  Dependency : IDependency
  Composition ..> Service : IService CreateService
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
/// <para>
/// Composition roots:<br/>
/// <list type="table">
/// <listheader>
/// <term>Root</term>
/// <description>Description</description>
/// </listheader>
/// <item>
/// <term>
/// <see cref="Pure.DI.UsageTests.Basics.RootArgumentsScenario.Service"/> CreateService
/// </term>
/// <description>
/// </description>
/// </item>
/// </list>
/// </para>
/// <example>
/// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Basics.RootArgumentsScenario.Service"/> using the composition root <see cref="CreateService"/>:
/// <code>
/// var composition = new Composition(id, dependencyName, serviceName);
/// var instance = composition.CreateService(id, dependencyName, serviceName);
/// </code>
/// </example>
/// <a href="https://mermaid.live/view#pako:eNqNU7FuwyAQ_RV0c4fIHtJmc-wOXrJ4ZaH21UUtEGFSqYry78VgB0xjq8uJ0-Me794dV2hVh3CA9osNQ8VZr5mgmkqXk1KJsxq44UoSetnt9scRG0_ZsW5Qf_MWSamRGZwyhz1zaQjv3Lkkg9Fc9qTDM8oOZftzYgKX2OCL78DL_My-CmJqafIspI0rHfPqzuxEZlWI-3KM-Supk0t5QQJVgsVdBsh35kSE3prV3pIWZrPWBcY3FupiIJa2cHwSIoO1ccdB34a9qy7kXmPJ3uy4WGu84ik-Ynqs-P80kRIVG-ZXYHao0P1FoN00CnYesFk42fO3MpmcY5nlb1FQeFd6ngCQmDJe5gWfIyoWpOnqWeZoDmNx-gUz55iPdm2SrVn5lJYHnkCgFox39rdfKZgPHPUdrAVMf1K4we0XFANdaw">Class diagram</a><br/>
/// This class was created by <a href="https://github.com/DevTeam/Pure.DI">Pure.DI</a> source code generator.
/// </summary>
/// <seealso cref="Pure.DI.DI.Setup"/>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Composition
{
  private readonly Composition _rootM03D07di;
  
  /// <summary>
  /// This parameterized constructor creates a new instance of <see cref="Composition"/> with arguments.
  /// </summary>
  public Composition()
  {
    _rootM03D07di = this;
  }
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/> scope based on <paramref name="baseComposition"/>. This allows the <see cref="Lifetime.Scoped"/> life time to be applied.
  /// </summary>
  /// <param name="baseComposition">Base composition.</param>
  internal Composition(Composition baseComposition)
  {
    _rootM03D07di = baseComposition._rootM03D07di;
  }
  
  #region Composition Roots
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public Pure.DI.UsageTests.Basics.RootArgumentsScenario.IService CreateService(int id, string dependencyName, string serviceName)
  {
    return new Pure.DI.UsageTests.Basics.RootArgumentsScenario.Service(serviceName, new Pure.DI.UsageTests.Basics.RootArgumentsScenario.Dependency(id, dependencyName));
  }
  #endregion
  
  /// <summary>
  /// This method provides a class diagram in mermaid format. To see this diagram, simply call the method and copy the text to this site https://mermaid.live/.
  /// </summary>
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +IService CreateService(int id, string dependencyName, string serviceName)\n" +
        "  }\n" +
        "  class Int32\n" +
        "  class String\n" +
        "  Dependency --|> IDependency : \n" +
        "  class Dependency {\n" +
          "    +Dependency(Int32 id, String dependencyName)\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(String name, IDependency dependency)\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Dependency o-- Int32 : Argument \"id\"\n" +
        "  Dependency o-- String : Argument \"dependencyName\"\n" +
        "  Service o-- String : \"forService\"  Argument \"serviceName\"\n" +
        "  Service *--  Dependency : IDependency\n" +
        "  Composition ..> Service : IService CreateService";
  }
}
```

</blockquote></details>

