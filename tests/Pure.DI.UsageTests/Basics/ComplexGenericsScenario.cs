/*
$v=true
$p=10
$d=Complex Generics
$h=Defining generic type arguments using particular marker types like ```TT``` in this sample is a distinguishing and outstanding feature. This allows binding complex generic types with nested generic types and with any type constraints. For instance ```IService<T1, T2, TList, TDictionary> where T2: struct where TList: IList<T1> where TDictionary: IDictionary<T1, T2> { }``` and its binding to the some implementation ```.Bind<IService<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>().To<Service<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()``` with all checks and code-generation at the compile time. It is clear that this example is exaggerated, it just demonstrates the ease of working with marker types like ```TT, TTEnumerable, TTSet``` and etc. for binding complex generic types.
$f=It can also be useful in a very simple scenario where, for example, the sequence of type arguments does not match the sequence of arguments of the contract that implements the type.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedTypeParameter
namespace Pure.DI.UsageTests.Basics.ComplexGenericsScenario;

using Shouldly;
using Xunit;

// {
internal interface IDependency<T> { }

internal class Dependency<T> : IDependency<T> { }

internal readonly record struct DependencyStruct<T> : IDependency<T> 
    where T: struct;

internal interface IService<T1, T2, TList, TDictionary>
    where T2: struct
    where TList: IList<T1>
    where TDictionary: IDictionary<T1, T2>
{
    IDependency<T1> Dependency1 { get; }
    
    IDependency<T2> Dependency2 { get; }
}

internal class Service<T1, T2, TList, TDictionary> : IService<T1, T2, TList, TDictionary>
    where T2: struct
    where TList: IList<T1>
    where TDictionary: IDictionary<T1, T2>
{
    public Service(
        IDependency<T1> dependency1,
        [Tag("value type")] IDependency<T2> dependency2)
    {
        Dependency1 = dependency1;
        Dependency2 = dependency2;
    }
    
    public IDependency<T1> Dependency1 { get; }
    
    public IDependency<T2> Dependency2 { get; }
}

internal class Program<T> where T : notnull
{
    public IService<T, int, List<T>, Dictionary<T, int>> Service { get; }

    public Program(IService<T, int, List<T>, Dictionary<T, int>> service)
    {
        Service = service;
    }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
// {            
        DI.Setup("Composition")
            .Bind<IDependency<TT>>().To<Dependency<TT>>()
            .Bind<IDependency<TTS>>("value type").To<DependencyStruct<TTS>>()
            .Bind<IService<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()
                .To<Service<TT1, TTS2, TTList<TT1>, TTDictionary<TT1, TTS2>>>()
            .Root<Program<string>>("Root");

        var composition = new Composition();
        var program = composition.Root;
        var service = program.Service;
        service.ShouldBeOfType<Service<string, int, List<string>, Dictionary<string, int>>>();
        service.Dependency1.ShouldBeOfType<Dependency<string>>();
        service.Dependency2.ShouldBeOfType<DependencyStruct<int>>();
// }
        TestTools.SaveClassDiagram(composition, nameof(ComplexGenericsScenario));
    }
}