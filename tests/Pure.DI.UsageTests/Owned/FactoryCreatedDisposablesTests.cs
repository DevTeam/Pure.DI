// Owned<T> behaviour comparison: disposables created on demand through a
// Func<T> factory that lives inside an Owned<T> graph.
//
// The root captures a Func<IWidget> and calls it three times, producing three
// independent disposable widgets. Disposing the Owned<T> must dispose ALL
// factory-created widgets (they were created within the owned graph).
//
// Reference implementation: Autofac (Func<T> resolves from the owning scope, so
// every produced instance is tracked and disposed with that scope).

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Local

#if AUTOFAC_REFERENCE
using Autofac;
#endif

namespace Pure.DI.UsageTests.Owned.FactoryCreatedDisposables;

public class FactoryCreatedDisposablesTests
{
    [Fact]
    public void Disposing_owned_disposes_every_factory_created_widget()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IRoot> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IRoot>>();
            IReadOnlyList<IWidget> widgets = owned.Value.Widgets;

            widgets.Count.ShouldBe(3);
            widgets.ShouldAllBe(widget => !widget.IsDisposed);

            owned.Dispose();
            widgets.ShouldAllBe(widget => widget.IsDisposed);
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IRoot> owned = composition.Root;
            IReadOnlyList<IWidget> widgets = owned.Value.Widgets;

            widgets.Count.ShouldBe(3);
            widgets.ShouldAllBe(widget => !widget.IsDisposed);

            owned.Dispose();
            widgets.ShouldAllBe(widget => widget.IsDisposed);
        }
    }

    [Fact]
    public void Factory_produces_distinct_widget_instances()
    {
#if AUTOFAC_REFERENCE
        {
            using IContainer container = BuildAutofac();
            Autofac.Features.OwnedInstances.Owned<IRoot> owned =
                container.Resolve<Autofac.Features.OwnedInstances.Owned<IRoot>>();
            IReadOnlyList<IWidget> widgets = owned.Value.Widgets;

            widgets.Distinct().Count().ShouldBe(3);
        }
#endif
        {
            Composition composition = new Composition();
            Owned<IRoot> owned = composition.Root;
            IReadOnlyList<IWidget> widgets = owned.Value.Widgets;

            widgets.Distinct().Count().ShouldBe(3);
        }
    }

#if AUTOFAC_REFERENCE
    private static IContainer BuildAutofac()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterType<Widget>().As<IWidget>().InstancePerDependency();
        builder.RegisterType<Root>().As<IRoot>().InstancePerDependency();
        return builder.Build();
    }
#endif
}

interface IWidget
{
    bool IsDisposed { get; }
}

sealed class Widget : IWidget, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IRoot
{
    IReadOnlyList<IWidget> Widgets { get; }
}

sealed class Root : IRoot
{
    public Root(Func<IWidget> widgetFactory) =>
        Widgets = [widgetFactory(), widgetFactory(), widgetFactory()];

    public IReadOnlyList<IWidget> Widgets { get; }
}

partial class Composition
{
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind().To<Widget>()
            .Bind().To<Root>()
            .Root<Owned<IRoot>>("Root");
}
