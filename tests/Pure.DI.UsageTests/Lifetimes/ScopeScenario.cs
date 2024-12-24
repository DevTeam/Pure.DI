﻿/*
$v=true
$p=4
$d=Scope
$h=The _Scoped_ lifetime ensures that there will be a single instance of the dependency for each scope.
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

// ReSharper disable PartialTypeWithSinglePart
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Lifetimes.ScopeScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var composition = new Composition();
        var program = composition.ProgramRoot;

        // Creates session #1
        var session1 = program.CreateSession();
        var dependency1 = session1.SessionRoot.Dependency;
        var dependency12 = session1.SessionRoot.Dependency;

        // Checks the identity of scoped instances in the same session
        dependency1.ShouldBe(dependency12);

        // Creates session #2
        var session2 = program.CreateSession();
        var dependency2 = session2.SessionRoot.Dependency;

        // Checks that the scoped instances are not identical in different sessions
        dependency1.ShouldNotBe(dependency2);

        // Disposes of session #1
        session1.Dispose();
        // Checks that the scoped instance is finalized
        dependency1.IsDisposed.ShouldBeTrue();

        // Disposes of session #2
        session2.Dispose();
        // Checks that the scoped instance is finalized
        dependency2.IsDisposed.ShouldBeTrue();
// }
        composition.SaveClassDiagram();
    }
}

// {
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency => dependency;
}

// Implements a session
class Session(Composition composition) : Composition(composition);

partial class Program(Func<Session> sessionFactory)
{
    public Session CreateSession() => sessionFactory();
}

partial class Composition
{
    static void Setup() =>
        DI.Setup()
            // This hint indicates to not generate methods such as Resolve
            .Hint(Hint.Resolve, "Off")
            .Bind().As(Scoped).To<Dependency>()
            .Bind().To<Service>()

            // Session composition root
            .Root<IService>("SessionRoot")

            // Program composition root
            .Root<Program>("ProgramRoot");
}
// }