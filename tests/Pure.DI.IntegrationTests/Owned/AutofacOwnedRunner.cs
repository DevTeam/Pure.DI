// ReSharper disable MemberHidesStaticFromOuterClass
namespace Pure.DI.IntegrationTests.Owned;

using System.Diagnostics.CodeAnalysis;
using Autofac;
using Autofac.Features.OwnedInstances;

[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
static class AutofacOwnedRunner
{
    public static IReadOnlyList<string> Run(OwnedComparisonScenario scenario)
    {
        var tracker = new Tracker();
        var builder = new ContainerBuilder();
        builder.RegisterInstance(tracker);
        builder.RegisterType<Service>().As<IService>();
        builder.RegisterType<Inner>().As<IInner>();
        builder.RegisterType<Outer>().As<IOuter>();
        builder.RegisterType<Shared>().As<IShared>().InstancePerLifetimeScope();
        builder.RegisterType<Left>().As<ILeft>();
        builder.RegisterType<Right>().As<IRight>();
        builder.RegisterType<Consumer>().As<IConsumer>();
        builder.RegisterType<SingletonLog>().As<ISingletonLog>().SingleInstance();
        builder.RegisterType<Worker>().As<IWorker>();
        builder.RegisterType<Job>().As<IJob>();
        builder.RegisterType<App>().As<IApp>();
        using var container = builder.Build();

        return scenario switch
        {
            OwnedComparisonScenario.DisposeAllFactoryHandles =>
                DisposeAllFactoryHandles(container),
            OwnedComparisonScenario.DisposeOnlyMiddleFactoryHandle =>
                DisposeOnlyMiddleFactoryHandle(container),
            OwnedComparisonScenario.DisposeNestedInnerHandle =>
                DisposeNestedInnerHandle(container),
            OwnedComparisonScenario.DisposeNestedOuterHandle =>
                DisposeNestedOuterHandle(container),
            OwnedComparisonScenario.DisposeDiamondGraph =>
                DisposeDiamondGraph(container, tracker),
            OwnedComparisonScenario.DisposeMiddleTripleNestedHandle =>
                DisposeMiddleTripleNestedHandle(container),
            OwnedComparisonScenario.IsolateFactoryUnitsWithNestedHandles =>
                IsolateFactoryUnitsWithNestedHandles(container),
            OwnedComparisonScenario.DisposeMixedLifetimeGraph =>
                DisposeMixedLifetimeGraph(container),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
    }

    private static IReadOnlyList<string> DisposeAllFactoryHandles(IComponentContext context)
    {
        var factory = context.Resolve<Func<Owned<IService>>>();
        var first = factory();
        var second = factory();
        var third = factory();
        var firstValue = first.Value;
        var secondValue = second.Value;
        var thirdValue = third.Value;
        var result = new List<string>
        {
            $"distinct={Distinct(firstValue, secondValue, thirdValue)}"
        };

        first.Dispose();
        second.Dispose();
        third.Dispose();
        result.Add($"dispose-counts={firstValue.DisposeCount},{secondValue.DisposeCount},{thirdValue.DisposeCount}");
        return result;
    }

    private static IReadOnlyList<string> DisposeOnlyMiddleFactoryHandle(IComponentContext context)
    {
        var factory = context.Resolve<Func<Owned<IService>>>();
        var first = factory();
        var second = factory();
        var third = factory();
        var firstValue = first.Value;
        var secondValue = second.Value;
        var thirdValue = third.Value;

        second.Dispose();
        var result = new List<string>
        {
            $"after-middle={firstValue.DisposeCount},{secondValue.DisposeCount},{thirdValue.DisposeCount}"
        };

        first.Dispose();
        third.Dispose();
        result.Add($"after-cleanup={firstValue.DisposeCount},{secondValue.DisposeCount},{thirdValue.DisposeCount}");
        return result;
    }

    private static IReadOnlyList<string> DisposeNestedInnerHandle(IComponentContext context)
    {
        var outer = context.Resolve<Owned<IOuter>>();
        var outerValue = outer.Value;
        var inner = outerValue.Inner;
        var innerValue = inner.Value;

        inner.Dispose();
        var result = new List<string>
        {
            $"after-inner={innerValue.DisposeCount},{outerValue.DisposeCount}"
        };

        outer.Dispose();
        result.Add($"after-outer={innerValue.DisposeCount},{outerValue.DisposeCount}");
        return result;
    }

    private static IReadOnlyList<string> DisposeNestedOuterHandle(IComponentContext context)
    {
        var outer = context.Resolve<Owned<IOuter>>();
        var outerValue = outer.Value;
        var inner = outerValue.Inner;
        var innerValue = inner.Value;

        outer.Dispose();
        var result = new List<string>
        {
            $"after-outer={innerValue.DisposeCount},{outerValue.DisposeCount}"
        };

        inner.Dispose();
        result.Add($"after-inner-cleanup={innerValue.DisposeCount},{outerValue.DisposeCount}");
        return result;
    }

    private static IReadOnlyList<string> DisposeDiamondGraph(IComponentContext context, Tracker tracker)
    {
        var owned = context.Resolve<Owned<IConsumer>>();
        var consumer = owned.Value;
        var left = consumer.Left;
        var right = consumer.Right;
        var shared = left.Shared;
        var result = new List<string>
        {
            $"shared={ReferenceEquals(shared, right.Shared)}"
        };

        owned.Dispose();
        result.Add($"dispose-counts={consumer.DisposeCount},{left.DisposeCount},{right.DisposeCount},{shared.DisposeCount}");
        result.Add($"dispose-order={string.Join(",", tracker.DisposalOrder)}");
        return result;
    }

    private static IReadOnlyList<string> DisposeMiddleTripleNestedHandle(IComponentContext context)
    {
        var outer = context.Resolve<Owned<Owned<Owned<IService>>>>();
        var middle = outer.Value;
        var inner = middle.Value;
        var value = inner.Value;

        middle.Dispose();
        var result = new List<string> { $"after-middle={value.DisposeCount}" };
        outer.Dispose();
        result.Add($"after-outer={value.DisposeCount}");
        inner.Dispose();
        result.Add($"after-inner={value.DisposeCount}");
        return result;
    }

    private static IReadOnlyList<string> IsolateFactoryUnitsWithNestedHandles(IComponentContext context)
    {
        var factory = context.Resolve<Func<Owned<IOuter>>>();
        var first = factory();
        var second = factory();
        var firstOuter = first.Value;
        var secondOuter = second.Value;
        var firstInner = firstOuter.Inner;
        var secondInner = secondOuter.Inner;
        var firstInnerValue = firstInner.Value;
        var secondInnerValue = secondInner.Value;

        firstInner.Dispose();
        var result = new List<string>
        {
            $"after-first-inner={firstInnerValue.DisposeCount},{firstOuter.DisposeCount},{secondInnerValue.DisposeCount},{secondOuter.DisposeCount}"
        };

        first.Dispose();
        result.Add($"after-first-outer={firstInnerValue.DisposeCount},{firstOuter.DisposeCount},{secondInnerValue.DisposeCount},{secondOuter.DisposeCount}");
        second.Dispose();
        result.Add($"after-second-outer={firstInnerValue.DisposeCount},{firstOuter.DisposeCount},{secondInnerValue.DisposeCount},{secondOuter.DisposeCount}");
        secondInner.Dispose();
        result.Add($"after-cleanup={firstInnerValue.DisposeCount},{firstOuter.DisposeCount},{secondInnerValue.DisposeCount},{secondOuter.DisposeCount}");
        return result;
    }

    private static IReadOnlyList<string> DisposeMixedLifetimeGraph(IContainer container)
    {
        var owned = container.Resolve<Owned<IApp>>();
        var app = owned.Value;
        var nestedJob = app.OwnedJob;
        var nestedJobValue = nestedJob.Value;
        var extraJob = app.JobFactory();
        var extraJobValue = extraJob.Value;

        extraJob.Dispose();
        var result = new List<string>
        {
            $"after-extra={app.DisposeCount},{app.Worker.DisposeCount},{nestedJobValue.DisposeCount},{extraJobValue.DisposeCount},{app.Log.DisposeCount}"
        };

        owned.Dispose();
        result.Add($"after-app={app.DisposeCount},{app.Worker.DisposeCount},{nestedJobValue.DisposeCount},{extraJobValue.DisposeCount},{app.Log.DisposeCount}");
        nestedJob.Dispose();
        result.Add($"after-nested={app.DisposeCount},{app.Worker.DisposeCount},{nestedJobValue.DisposeCount},{extraJobValue.DisposeCount},{app.Log.DisposeCount}");
        container.Dispose();
        result.Add($"after-container={app.DisposeCount},{app.Worker.DisposeCount},{nestedJobValue.DisposeCount},{extraJobValue.DisposeCount},{app.Log.DisposeCount}");
        return result;
    }

    private static bool Distinct(params object[] values) =>
        values.Distinct(ReferenceEqualityComparer.Instance).Count() == values.Length;

    private sealed class Tracker
    {
        public List<string> DisposalOrder { get; } = [];

        public void Disposed(string kind) => DisposalOrder.Add(kind);
    }

    private abstract class Tracked(Tracker tracker, string kind) : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            tracker.Disposed(kind);
        }
    }

    private interface IService
    {
        int DisposeCount { get; }
    }

    private sealed class Service(Tracker tracker) : Tracked(tracker, "Service"), IService;

    private interface IInner
    {
        int DisposeCount { get; }
    }

    private sealed class Inner(Tracker tracker) : Tracked(tracker, "Inner"), IInner;

    private interface IOuter
    {
        Owned<IInner> Inner { get; }

        int DisposeCount { get; }
    }

    private sealed class Outer(Tracker tracker, Owned<IInner> inner) : Tracked(tracker, "Outer"), IOuter
    {
        public Owned<IInner> Inner { get; } = inner;
    }

    private interface IShared
    {
        int DisposeCount { get; }
    }

    private sealed class Shared(Tracker tracker) : Tracked(tracker, "Shared"), IShared;

    private interface ILeft
    {
        IShared Shared { get; }

        int DisposeCount { get; }
    }

    private sealed class Left(Tracker tracker, IShared shared) : Tracked(tracker, "Left"), ILeft
    {
        public IShared Shared { get; } = shared;
    }

    private interface IRight
    {
        IShared Shared { get; }

        int DisposeCount { get; }
    }

    private sealed class Right(Tracker tracker, IShared shared) : Tracked(tracker, "Right"), IRight
    {
        public IShared Shared { get; } = shared;
    }

    private interface IConsumer
    {
        ILeft Left { get; }

        IRight Right { get; }

        int DisposeCount { get; }
    }

    private sealed class Consumer(Tracker tracker, ILeft left, IRight right) : Tracked(tracker, "Consumer"), IConsumer
    {
        public ILeft Left { get; } = left;

        public IRight Right { get; } = right;
    }

    private interface ISingletonLog
    {
        int DisposeCount { get; }
    }

    private sealed class SingletonLog(Tracker tracker) : Tracked(tracker, "SingletonLog"), ISingletonLog;

    private interface IWorker
    {
        int DisposeCount { get; }
    }

    private sealed class Worker(Tracker tracker) : Tracked(tracker, "Worker"), IWorker;

    private interface IJob
    {
        int DisposeCount { get; }
    }

    private sealed class Job(Tracker tracker) : Tracked(tracker, "Job"), IJob;

    private interface IApp
    {
        ISingletonLog Log { get; }

        IWorker Worker { get; }

        Owned<IJob> OwnedJob { get; }

        Func<Owned<IJob>> JobFactory { get; }

        int DisposeCount { get; }
    }

    private sealed class App(
        Tracker tracker,
        ISingletonLog log,
        IWorker worker,
        Owned<IJob> ownedJob,
        Func<Owned<IJob>> jobFactory)
        : Tracked(tracker, "App"), IApp
    {
        public ISingletonLog Log { get; } = log;

        public IWorker Worker { get; } = worker;

        public Owned<IJob> OwnedJob { get; } = ownedJob;

        public Func<Owned<IJob>> JobFactory { get; } = jobFactory;
    }
}
