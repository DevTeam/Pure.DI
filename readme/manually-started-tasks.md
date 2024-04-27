#### Manually started tasks

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/BaseClassLibrary/ManualTaskScenario.cs)

By default, tasks are started automatically when they are injected. But you can override this behavior as shown in the example below. It is also recommended to add a binding for <c>CancellationToken</c> to be able to cancel the execution of a task.

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

class Service : IService
{
    private readonly Task<IDependency> _dependencyTask;

    public Service(Task<IDependency> dependencyTask)
    {
        _dependencyTask = dependencyTask;
        // This is where the task starts
        _dependencyTask.Start();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var dependency = await _dependencyTask;
        await dependency.DoSomething(cancellationToken);
    }
}

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    // Overrides the default binding that performs an auto-start of a task
    // when it is created. This binding will simply create the task.
    // The start will be handled by the consumer.
    .Bind<Task<TT>>().To(ctx =>
    {
        ctx.Inject(ctx.Tag, out Func<TT> factory);
        ctx.Inject(out CancellationToken cancellationToken);
        return new Task<TT>(factory, cancellationToken);
    })
    // Specifies to use CancellationToken from the composition root argument,
    // if not specified then CancellationToken.None will be used
    .RootArg<CancellationToken>("cancellationToken")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()

    // Composition root
    .Root<IService>("GetRoot");

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
  class TaskFactory
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
  Composition ..> Service : IService GetRoot(System.Threading.CancellationToken cancellationToken)
  TaskᐸIDependencyᐳ o--  "PerResolve" FuncᐸIDependencyᐳ : FuncᐸIDependencyᐳ
  TaskᐸIDependencyᐳ o-- CancellationToken : Argument "cancellationToken"
  FuncᐸIDependencyᐳ *--  Dependency : IDependency
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
partial class Composition
{
  private readonly Composition _rootM04D27di;
  private readonly object _lockM04D27di;
  
  public Composition()
  {
    _rootM04D27di = this;
    _lockM04D27di = new object();
  }
  
  internal Composition(Composition baseComposition)
  {
    _rootM04D27di = baseComposition._rootM04D27di;
    _lockM04D27di = _rootM04D27di._lockM04D27di;
  }
  
  [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)0x100)]
  public Pure.DI.UsageTests.BCL.ManualTaskScenario.IService GetRoot(System.Threading.CancellationToken cancellationToken)
  {
    var perResolveM04D27di42_Func = default(System.Func<Pure.DI.UsageTests.BCL.ManualTaskScenario.IDependency>);
    perResolveM04D27di42_Func = new global::System.Func<Pure.DI.UsageTests.BCL.ManualTaskScenario.IDependency>(
    [global::System.Runtime.CompilerServices.MethodImpl((global::System.Runtime.CompilerServices.MethodImplOptions)768)]
    () =>
    {
        var value_M04D27di1 = new Pure.DI.UsageTests.BCL.ManualTaskScenario.Dependency();
        return value_M04D27di1;
    });
    System.Threading.Tasks.Task<Pure.DI.UsageTests.BCL.ManualTaskScenario.IDependency> transientM04D27di1_Task;
    {
        var factory_M04D27di2 = perResolveM04D27di42_Func;
        var cancellationToken_M04D27di3 = cancellationToken;
        transientM04D27di1_Task = new Task<Pure.DI.UsageTests.BCL.ManualTaskScenario.IDependency>(factory_M04D27di2, cancellationToken_M04D27di3);
    }
    return new Pure.DI.UsageTests.BCL.ManualTaskScenario.Service(transientM04D27di1_Task);
  }
  
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +IService GetRoot(System.Threading.CancellationToken cancellationToken)\n" +
        "  }\n" +
        "  class TaskFactory\n" +
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
        "  Composition ..> Service : IService GetRoot(System.Threading.CancellationToken cancellationToken)\n" +
        "  TaskᐸIDependencyᐳ o--  \"PerResolve\" FuncᐸIDependencyᐳ : FuncᐸIDependencyᐳ\n" +
        "  TaskᐸIDependencyᐳ o-- CancellationToken : Argument \"cancellationToken\"\n" +
        "  FuncᐸIDependencyᐳ *--  Dependency : IDependency";
  }
  
}
```

</blockquote></details>

