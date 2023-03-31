﻿/*
$v=true
$p=2
$d=Array
$h=Specifying `T[]` as the injection type allows instances from all bindings that implement the `T` type to be injected.
$f=In addition to arrays, other collection types are also supported, such as:
$f=- System.Memory<T>
$f=- System.ReadOnlyMemory<T>
$f=- System.Span<T>
$f=- System.ReadOnlySpan<T>
$f=- System.Collections.Generic.ICollection<T>
$f=- System.Collections.Generic.IList<T>
$f=- System.Collections.Generic.List<T>
$f=- System.Collections.Generic.IReadOnlyCollection<T>
$f=- System.Collections.Generic.IReadOnlyList<T>
$f=- System.Collections.Generic.ISet<T>
$f=- System.Collections.Generic.HashSet<T>
$f=- System.Collections.Generic.SortedSet<T>
$f=- System.Collections.Generic.Queue<T>
$f=- System.Collections.Generic.Stack<T>
$f=- System.Collections.Immutable.ImmutableArray<T>
$f=- System.Collections.Immutable.IImmutableList<T>
$f=- System.Collections.Immutable.ImmutableList<T>
$f=- System.Collections.Immutable.IImmutableSet<T>
$f=- System.Collections.Immutable.ImmutableHashSet<T>
$f=- System.Collections.Immutable.ImmutableSortedSet<T>
$f=- System.Collections.Immutable.IImmutableQueue<T>
$f=- System.Collections.Immutable.ImmutableQueue<T>
$f=- System.Collections.Immutable.IImmutableStack<T>
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
namespace Pure.DI.UsageTests.BCL.ArrayScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
internal interface IDependency { }

internal class AbcDependency : IDependency { }

internal class XyzDependency : IDependency { }

internal interface IService
{
    IDependency[] Dependencies { get; }
}

internal class Service : IService
{
    public Service(IDependency[] dependencies)
    {
        Dependencies = dependencies;
    }

    public IDependency[] Dependencies { get; }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        DI.Setup("Composition")
            .Bind<IDependency>().To<AbcDependency>()
            .Bind<IDependency>(2).To<XyzDependency>()
            .Bind<IService>().To<Service>()
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependencies.Length.ShouldBe(2);
        service.Dependencies[0].ShouldBeOfType<AbcDependency>();
        service.Dependencies[1].ShouldBeOfType<XyzDependency>();
// }            
    }
}