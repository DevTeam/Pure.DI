// Owned<T> behaviour comparison: disposal ORDER within an Owned<T> graph.
//
// A 3-level chain A -> B -> C (all disposable, all transient). A shared recorder
// (injected as a composition argument / registered instance) records the order
// in which Dispose() is called when the Owned<T> is disposed.
//
// The expected order literal is set to what Autofac (the reference) actually
// does and then checked against Pure.DI.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.DisposalOrder;

public class DisposalOrderTests
{
    [Fact]
    public void Owned_disposes_the_graph_in_reverse_construction_order()
    {
#if AUTOFAC_REFERENCE
        {
            DisposalRecorder recorder = new DisposalRecorder();
            using IContainer container = BuildAutofac(recorder);
            Autofac.Features.OwnedInstances.Owned<IA> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IA>>();
            _ = owned.Value.B.C;

            owned.Dispose();
            recorder.Names.ShouldBe(["A", "B", "C"]);
        }
#endif
        {
            DisposalRecorder recorder = new DisposalRecorder();
            Composition composition = new Composition(recorder);
            Owned<IA> owned = composition.Root;
            _ = owned.Value.B.C;

            owned.Dispose();
            recorder.Names.ShouldBe(["A", "B", "C"]);
        }
    }

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac(DisposalRecorder recorder)
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterInstance(recorder);
        builder.RegisterType<C>().As<IC>().InstancePerDependency();
        builder.RegisterType<B>().As<IB>().InstancePerDependency();
        builder.RegisterType<A>().As<IA>().InstancePerDependency();
        return builder.Build();
    }
#endif
}

sealed class DisposalRecorder
{
    private readonly List<string> names = [];

    public IReadOnlyList<string> Names => names;

    public void Record(string name) => names.Add(name);
}

interface IC;

sealed class C(DisposalRecorder recorder) : IC, IDisposable
{
    public void Dispose() => recorder.Record("C");
}

interface IB
{
    IC C { get; }
}

sealed class B(IC c, DisposalRecorder recorder) : IB, IDisposable
{
    public IC C { get; } = c;

    public void Dispose() => recorder.Record("B");
}

interface IA
{
    IB B { get; }
}

sealed class A(IB b, DisposalRecorder recorder) : IA, IDisposable
{
    public IB B { get; } = b;

    public void Dispose() => recorder.Record("A");
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Arg<DisposalRecorder>("recorder")
            .Bind().To<C>()
            .Bind().To<B>()
            .Bind().To<A>()
            .Root<Owned<IA>>("Root");
}
