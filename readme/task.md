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
  Composition ..> Service : IService GetRoot(System.Threading.CancellationToken cancellationToken)<br/>provides IService
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
/// <b>Composition roots</b><br/>
/// <list type="table">
/// <listheader>
/// <term>Root</term>
/// <description>Description</description>
/// </listheader>
/// <item>
/// <term>
/// <see cref="Pure.DI.UsageTests.BCL.TaskScenario.IService"/> <see cref="GetRoot(System.Threading.CancellationToken)"/>
/// </term>
/// <description>
/// Provides a composition root of type <see cref="Pure.DI.UsageTests.BCL.TaskScenario.Service"/>.
/// </description>
/// </item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.BCL.TaskScenario.Service"/> using the composition root <see cref="GetRoot(System.Threading.CancellationToken)"/>:
/// <code>
/// var composition = new Composition();
/// var instance = composition.GetRoot(cancellationToken);
/// </code>
/// </example>
/// <a href="https://mermaid.live/view#pako:eNq1VktuwjAQvYrldaUiWNCygwQqVq2AZTbGGUFEYkeOg4QQd-Au3fQ63KROQojjfEhLu7Fsz-TN7z0rR0y5C3iEqU-iyPbIRpDAEQ5Lz8jiQcgjT3qcISfu9YaTxJbs-pP5EsTeo4DeQC44l-nty_IQSQjS_XS1FUBcj22yo0UYBd8nCdqK74Ahat6kjq95jKFdZLIi0c5ScInre5iskWHkTHosbnaYESq5OJQvl3QLbuyD0GquJKVMNoTAXGD0kPahbxfr0ErWwRTNDafBGBWohk1vZGHKWljXhLzXzcF1j1Jk3aCHvd5nMZNeXM5fWgmX8ydyb6fE3j6cyteFeRYz2mLWhtPiZXa3KGWQ9cAi60gKhZN15LrWZFtq1W9gtIQR1ydS5U4-jrHYxAEwiRxcJT02QVO0cQm5TgA5eIM2OiFWVVNCrRdVF-SbsnS8ktxKnK5BqPJRQ6plivlc9dPxZavSiCGRf3zAUiKthfOcRQ4F33suFNTLm1gtscwnRZcPEAuIuL8HB9crKa-nUWbdI018TncqTpsk9Sm0ybY5V3PY1XdT-8jg2706_kaB3fJ-SJI_CPGoRruHui9a_IQDEAHxXPXTcHSw3EKgmDlysEuEos4Jn74BrLkPww">Class diagram</a><br/>
/// This class was created by <a href="https://github.com/DevTeam/Pure.DI">Pure.DI</a> source code generator.
/// <seealso cref="Pure.DI.DI.Setup"/>
/// <seealso cref="Pure.DI.IConfiguration.Bind(object[])"/>
/// <seealso cref="Pure.DI.IConfiguration.Bind{T}(object[])"/>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Composition
{
  private readonly Composition _rootM03D14di;
  private readonly object _lockM03D14di;
  
  /// <summary>
  /// This parameterized constructor creates a new instance of <see cref="Composition"/> with arguments.
  /// </summary>
  public Composition()
  {
    _rootM03D14di = this;
    _lockM03D14di = new object();
  }
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/> scope based on <paramref name="baseComposition"/>. This allows the <see cref="Lifetime.Scoped"/> life time to be applied.
  /// </summary>
  /// <param name="baseComposition">Base composition.</param>
  internal Composition(Composition baseComposition)
  {
    _rootM03D14di = baseComposition._rootM03D14di;
    _lockM03D14di = _rootM03D14di._lockM03D14di;
  }
  
  #region Composition Roots
  /// <summary>
  /// Provides a composition root of type <see cref="Pure.DI.UsageTests.BCL.TaskScenario.Service"/>.
  /// </summary>
  /// <example>
  /// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.BCL.TaskScenario.Service"/>:
  /// <code>
  /// var composition = new Composition();
  /// var instance = composition.GetRoot(cancellationToken);
  /// </code>
  /// </example>
  #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
  [global::System.Diagnostics.Contracts.Pure]
  #endif
  public Pure.DI.UsageTests.BCL.TaskScenario.IService GetRoot(System.Threading.CancellationToken cancellationToken)
  {
    var perResolveM03D14di39_Func = default(System.Func<Pure.DI.UsageTests.BCL.TaskScenario.IDependency>);
    System.Threading.Tasks.TaskScheduler transientM03D14di5_TaskScheduler = TaskScheduler.Current;
    System.Threading.Tasks.TaskContinuationOptions transientM03D14di4_TaskContinuationOptions = global::System.Threading.Tasks.TaskContinuationOptions.None;
    System.Threading.Tasks.TaskCreationOptions transientM03D14di3_TaskCreationOptions = global::System.Threading.Tasks.TaskCreationOptions.None;
    System.Threading.Tasks.TaskFactory<Pure.DI.UsageTests.BCL.TaskScenario.IDependency> perBlockM03D14di2_TaskFactory;
    {
        var cancellationToken_M03D14di1 = cancellationToken;
        var taskCreationOptions_M03D14di2 = transientM03D14di3_TaskCreationOptions;
        var taskContinuationOptions_M03D14di3 = transientM03D14di4_TaskContinuationOptions;
        var taskScheduler_M03D14di4 = transientM03D14di5_TaskScheduler;
        perBlockM03D14di2_TaskFactory = new global::System.Threading.Tasks.TaskFactory<Pure.DI.UsageTests.BCL.TaskScenario.IDependency>(cancellationToken_M03D14di1, taskCreationOptions_M03D14di2, taskContinuationOptions_M03D14di3, taskScheduler_M03D14di4);
    }
    perResolveM03D14di39_Func = new global::System.Func<Pure.DI.UsageTests.BCL.TaskScenario.IDependency>(
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)768)]
    () =>
    {
        var factory_M03D14di5 = new Pure.DI.UsageTests.BCL.TaskScenario.Dependency();
        return factory_M03D14di5;
    });
    System.Threading.Tasks.Task<Pure.DI.UsageTests.BCL.TaskScenario.IDependency> transientM03D14di1_Task;
    {
        var factory_M03D14di6 = perResolveM03D14di39_Func;
        var taskFactory_M03D14di7 = perBlockM03D14di2_TaskFactory;
        transientM03D14di1_Task = taskFactory_M03D14di7.StartNew(factory_M03D14di6);
    }
    return new Pure.DI.UsageTests.BCL.TaskScenario.Service(transientM03D14di1_Task);
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
        "  Composition ..> Service : IService GetRoot(System.Threading.CancellationToken cancellationToken)<br/>provides IService\n" +
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

