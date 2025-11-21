/*
$v=true
$p=3
$d=Complex generics
$h=Defining generic type arguments using particular marker types like ```TT``` in this sample is a distinguishing and outstanding feature. This allows binding complex generic types with nested generic types and with any type constraints. For instance ```IService<T1, T2, TList, TDictionary> where T2: struct where TList: IList<T1> where TDictionary: IDictionary<T1, T2> { }``` and its binding to the some implementation ```.Bind<IService<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>().To<Service<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()``` with all checks and code-generation at the compile time. It is clear that this example is exaggerated, it just demonstrates the ease of working with marker types like ```TT, TTEnumerable, TTSet``` and etc. for binding complex generic types.
$f=It can also be useful in a very simple scenario where, for example, the sequence of type arguments does not match the sequence of arguments of the contract that implements the type.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedTypeParameter
// ReSharper disable ArrangeTypeModifiers

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Generics.ComplexGenericsScenario;

using Shouldly;
using Xunit;

// {
//# using Pure.DI;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .RootArg<TT>("name")
            .Bind<IConsumer<TT>>().To<Consumer<TT>>()
            .Bind<IConsumer<TTS>>("struct")
            .As(Lifetime.Singleton)
            .To<StructConsumer<TTS>>()
            .Bind<IWorkflow<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()
            .To<Workflow<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()

            // Composition root
            .Root<Program<TT>>("GetRoot");

        var composition = new Composition();
        var program = composition.GetRoot<string>(name: "Super Task");
        var workflow = program.Workflow;
        workflow.ShouldBeOfType<Workflow<string, int, List<string>, Dictionary<string, int>>>();
        workflow.TaskConsumer.ShouldBeOfType<Consumer<string>>();
        workflow.PriorityConsumer.ShouldBeOfType<StructConsumer<int>>();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IConsumer<T>;

class Consumer<T>(T name) : IConsumer<T>;

readonly record struct StructConsumer<T> : IConsumer<T>
    where T : struct;

interface IWorkflow<TTask, TPriority, TTaskList, TTaskPriorities>
    where TPriority : struct
    where TTaskList : IList<TTask>
    where TTaskPriorities : IDictionary<TTask, TPriority>
{
    IConsumer<TTask> TaskConsumer { get; }

    IConsumer<TPriority> PriorityConsumer { get; }
}

class Workflow<TTask, TPriority, TTaskList, TTaskPriorities>(
    IConsumer<TTask> taskConsumer,
    [Tag("struct")] IConsumer<TPriority> priorityConsumer)
    : IWorkflow<TTask, TPriority, TTaskList, TTaskPriorities>
    where TPriority : struct
    where TTaskList : IList<TTask>
    where TTaskPriorities : IDictionary<TTask, TPriority>
{
    public IConsumer<TTask> TaskConsumer { get; } = taskConsumer;

    public IConsumer<TPriority> PriorityConsumer { get; } = priorityConsumer;
}

class Program<T>(IWorkflow<T, int, List<T>, Dictionary<T, int>> workflow)
    where T : notnull
{
    public IWorkflow<T, int, List<T>, Dictionary<T, int>> Workflow { get; } = workflow;
}
// }