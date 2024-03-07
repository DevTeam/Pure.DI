#### Task

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/TaskScenario.cs)

By default, tasks are started automatically when they are injected. It is recommended to use an argument of type <c>CancellationToken</c> to the composition root to be able to cancel the execution of a task. In this case, the composition root property is automatically converted to a method with a parameter of type <c>CancellationToken</c>. To start a task, an instance of type <c>TaskFactory<T></c> is used, with default settings:

- CancellationToken.None
- TaskScheduler.Default
- TaskCreationOptions.None
- TaskContinuationOptions.None

But you can always override them, as in the example below for <c>TaskScheduler.Current</c>.

```c#
interface IDependency
{
    ValueTask DoSomething(CancellationToken cancellationToken);
}

class Dependency : IDependency
{
    public ValueTask DoSomething(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

interface IService
{
    Task RunAsync(CancellationToken cancellationToken);
}

class Service(Task<IDependency> dependencyTask) : IService
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var dependency = await dependencyTask;
        await dependency.DoSomething(cancellationToken);
    }
}

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    // Overrides TaskScheduler.Default if necessary
    .Bind<TaskScheduler>().To(_ => TaskScheduler.Current)
    // Specifies to use CancellationToken from the composition root argument,
    // if not specified then CancellationToken.None will be used
    .RootArg<CancellationToken>("cancellationToken")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>().Root<IService>("GetRoot");

var composition = new Composition();
using var cancellationTokenSource = new CancellationTokenSource();

// Creates a composition root with the CancellationToken passed to it
var service = composition.GetRoot(cancellationTokenSource.Token);
await service.RunAsync(cancellationTokenSource.Token);
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +IService GetRoot(System.Threading.CancellationToken cancellationToken)
  }
  class TaskCreationOptions
  class TaskContinuationOptions
  class TaskFactory
  class TaskScheduler
  class CancellationToken
  Dependency --|> IDependency : 
  class Dependency {
    +Dependency()
  }
  Service --|> IService : 
  class Service {
    +Service(TaskᐸIDependencyᐳ dependencyTask)
  }
  class TaskᐸIDependencyᐳ
  class FuncᐸIDependencyᐳ
  class TaskFactoryᐸIDependencyᐳ
  class IDependency {
    <<abstract>>
  }
  class IService {
    <<abstract>>
  }
  TaskFactory o-- CancellationToken : Argument "cancellationToken"
  TaskFactory *--  TaskCreationOptions : TaskCreationOptions
  TaskFactory *--  TaskContinuationOptions : TaskContinuationOptions
  TaskFactory *--  TaskScheduler : TaskScheduler
  Service *--  TaskᐸIDependencyᐳ : TaskᐸIDependencyᐳ
  Composition ..> Service : IService GetRoot
  TaskᐸIDependencyᐳ o--  "PerResolve" FuncᐸIDependencyᐳ : FuncᐸIDependencyᐳ
  TaskᐸIDependencyᐳ o--  "PerBlock" TaskFactoryᐸIDependencyᐳ : TaskFactoryᐸIDependencyᐳ
  FuncᐸIDependencyᐳ *--  Dependency : IDependency
  TaskFactoryᐸIDependencyᐳ o-- CancellationToken : Argument "cancellationToken"
  TaskFactoryᐸIDependencyᐳ *--  TaskCreationOptions : TaskCreationOptions
  TaskFactoryᐸIDependencyᐳ *--  TaskContinuationOptions : TaskContinuationOptions
  TaskFactoryᐸIDependencyᐳ *--  TaskScheduler : TaskScheduler
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
/// <see cref="Pure.DI.UsageTests.BCL.TaskScenario.Service"/> GetRoot
/// </term>
/// <description>
/// </description>
/// </item>
/// </list>
/// </para>
/// <example>
/// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.BCL.TaskScenario.Service"/> using the composition root <see cref="GetRoot"/>:
/// <code>
/// var composition = new Composition(cancellationToken);
/// var instance = composition.GetRoot(cancellationToken);
/// </code>
/// </example>
/// <a href="https://mermaid.live/view#pako:eNqtVUtuwjAQvYrldRcIFrTsIJSKVStgmY3rjCAisZHjICHEHbhLN70ON6mTEOJPEtLSjZV4Ju-N38xzjpjyAPAI04gkyTQka0FiX_gsf0cej3c8CWXIGfLTXm84yWLZU38yX4LYhxTQG8gF5zLffV4eEglx_vy62gggQcjWxatHGIUoIhnaim-BIWrv5IkvJcdwWlWyIsnWU3BZ6vsuWxMryJkMWdqcMCNUcnEwN5d0A0EagdDO7BSlQlPYAQuA0UOuQ39arUMvWwevaG4lDcaoQrViupBVqJCwToRS62ZyPcNg1gM67XW_4My0uJy_tSNczl8ouL1l8fbmOF9X4VnKaEtYa05Llq1udZRBoYFHPhMpFE6hyHWtqdaQ6i8wWsGI6x1xZ6dsx1is0xiYRD52hx7boDna2ECuM0AJ3uCNToiuawzUelN1Qb45S8cz7GbMdA2CO48aUu2k2NdVP29fsSqPWBZxLrDryVxes8mqhx8gFpDwaA8-rh_vkqRx9rszTSJOt4qnzSe6NG1eaq7V7oB7mWkfWUNw7xz_Y4tudT_kk19QPGqc7lT3nYSfcAwiJmGg_uRHH8sNxGoyRz4OiFCjc8KnH6s36eE">Class diagram</a><br/>
/// This class was created by <a href="https://github.com/DevTeam/Pure.DI">Pure.DI</a> source code generator.
/// </summary>
/// <seealso cref="Pure.DI.DI.Setup"/>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Composition
{
  private readonly Composition _rootM03D07di;
  private readonly object _lockM03D07di;
  
  /// <summary>
  /// This parameterized constructor creates a new instance of <see cref="Composition"/> with arguments.
  /// </summary>
  public Composition()
  {
    _rootM03D07di = this;
    _lockM03D07di = new object();
  }
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/> scope based on <paramref name="baseComposition"/>. This allows the <see cref="Lifetime.Scoped"/> life time to be applied.
  /// </summary>
  /// <param name="baseComposition">Base composition.</param>
  internal Composition(Composition baseComposition)
  {
    _rootM03D07di = baseComposition._rootM03D07di;
    _lockM03D07di = _rootM03D07di._lockM03D07di;
  }
  
  #region Composition Roots
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public Pure.DI.UsageTests.BCL.TaskScenario.IService GetRoot(System.Threading.CancellationToken cancellationToken)
  {
    var perResolveM03D07di39_Func = default(System.Func<Pure.DI.UsageTests.BCL.TaskScenario.IDependency>);
    System.Threading.Tasks.TaskScheduler transientM03D07di5_TaskScheduler = TaskScheduler.Current;
    System.Threading.Tasks.TaskContinuationOptions transientM03D07di4_TaskContinuationOptions = global::System.Threading.Tasks.TaskContinuationOptions.None;
    System.Threading.Tasks.TaskCreationOptions transientM03D07di3_TaskCreationOptions = global::System.Threading.Tasks.TaskCreationOptions.None;
    System.Threading.Tasks.TaskFactory<Pure.DI.UsageTests.BCL.TaskScenario.IDependency> perBlockM03D07di2_TaskFactory;
    {
        var cancellationToken_M03D07di1 = cancellationToken;
        var taskCreationOptions_M03D07di2 = transientM03D07di3_TaskCreationOptions;
        var taskContinuationOptions_M03D07di3 = transientM03D07di4_TaskContinuationOptions;
        var taskScheduler_M03D07di4 = transientM03D07di5_TaskScheduler;
        perBlockM03D07di2_TaskFactory = new global::System.Threading.Tasks.TaskFactory<Pure.DI.UsageTests.BCL.TaskScenario.IDependency>(cancellationToken_M03D07di1, taskCreationOptions_M03D07di2, taskContinuationOptions_M03D07di3, taskScheduler_M03D07di4);
    }
    perResolveM03D07di39_Func = new global::System.Func<Pure.DI.UsageTests.BCL.TaskScenario.IDependency>(
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)768)]
    () =>
    {
        var factory_M03D07di5 = new Pure.DI.UsageTests.BCL.TaskScenario.Dependency();
        return factory_M03D07di5;
    });
    System.Threading.Tasks.Task<Pure.DI.UsageTests.BCL.TaskScenario.IDependency> transientM03D07di1_Task;
    {
        var factory_M03D07di6 = perResolveM03D07di39_Func;
        var taskFactory_M03D07di7 = perBlockM03D07di2_TaskFactory;
        transientM03D07di1_Task = taskFactory_M03D07di7.StartNew(factory_M03D07di6);
    }
    return new Pure.DI.UsageTests.BCL.TaskScenario.Service(transientM03D07di1_Task);
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
          "    +IService GetRoot(System.Threading.CancellationToken cancellationToken)\n" +
        "  }\n" +
        "  class TaskCreationOptions\n" +
        "  class TaskContinuationOptions\n" +
        "  class TaskFactory\n" +
        "  class TaskScheduler\n" +
        "  class CancellationToken\n" +
        "  Dependency --|> IDependency : \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  Service --|> IService : \n" +
        "  class Service {\n" +
          "    +Service(TaskᐸIDependencyᐳ dependencyTask)\n" +
        "  }\n" +
        "  class TaskᐸIDependencyᐳ\n" +
        "  class FuncᐸIDependencyᐳ\n" +
        "  class TaskFactoryᐸIDependencyᐳ\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IService {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  TaskFactory o-- CancellationToken : Argument \"cancellationToken\"\n" +
        "  TaskFactory *--  TaskCreationOptions : TaskCreationOptions\n" +
        "  TaskFactory *--  TaskContinuationOptions : TaskContinuationOptions\n" +
        "  TaskFactory *--  TaskScheduler : TaskScheduler\n" +
        "  Service *--  TaskᐸIDependencyᐳ : TaskᐸIDependencyᐳ\n" +
        "  Composition ..> Service : IService GetRoot\n" +
        "  TaskᐸIDependencyᐳ o--  \"PerResolve\" FuncᐸIDependencyᐳ : FuncᐸIDependencyᐳ\n" +
        "  TaskᐸIDependencyᐳ o--  \"PerBlock\" TaskFactoryᐸIDependencyᐳ : TaskFactoryᐸIDependencyᐳ\n" +
        "  FuncᐸIDependencyᐳ *--  Dependency : IDependency\n" +
        "  TaskFactoryᐸIDependencyᐳ o-- CancellationToken : Argument \"cancellationToken\"\n" +
        "  TaskFactoryᐸIDependencyᐳ *--  TaskCreationOptions : TaskCreationOptions\n" +
        "  TaskFactoryᐸIDependencyᐳ *--  TaskContinuationOptions : TaskContinuationOptions\n" +
        "  TaskFactoryᐸIDependencyᐳ *--  TaskScheduler : TaskScheduler";
  }
}
```

</blockquote></details>

