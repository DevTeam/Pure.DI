namespace Pure.DI.IntegrationTests.Owned;

using Pure.DI.IntegrationTests;
using System.Globalization;

internal static class PureDiOwnedRunner
{
    public static async Task<IReadOnlyList<string>> RunAsync(OwnedComparisonScenario scenario)
    {
        var source = """
                     using System;
                     using System.Collections.Generic;
                     using System.Linq;
                     using Pure.DI;
                     using static Pure.DI.Lifetime;

                     namespace Sample
                     {
                         public static class Program
                         {
                             public static void Main()
                             {
                                 var scenario = int.Parse("#scenario#");
                                 switch (scenario)
                                 {
                                     case 1: DisposeAllFactoryHandles(); break;
                                     case 2: DisposeOnlyMiddleFactoryHandle(); break;
                                     case 3: DisposeNestedInnerHandle(); break;
                                     case 4: DisposeNestedOuterHandle(); break;
                                     case 5: DisposeDiamondGraph(); break;
                                     case 6: DisposeMiddleTripleNestedHandle(); break;
                                     case 7: IsolateFactoryUnitsWithNestedHandles(); break;
                                     case 8: DisposeMixedLifetimeGraph(); break;
                                     default: throw new ArgumentOutOfRangeException();
                                 }
                             }

                             private static void DisposeAllFactoryHandles()
                             {
                                 var composition = new Composition();
                                 Func<Owned<IService>> factory = composition.ServiceFactory;
                                 Owned<IService> first = factory();
                                 Owned<IService> second = factory();
                                 Owned<IService> third = factory();
                                 IService firstValue = first.Value;
                                 IService secondValue = second.Value;
                                 IService thirdValue = third.Value;

                                 Console.WriteLine("distinct=" + Distinct(firstValue, secondValue, thirdValue));
                                 first.Dispose();
                                 second.Dispose();
                                 third.Dispose();
                                 Console.WriteLine($"dispose-counts={firstValue.DisposeCount},{secondValue.DisposeCount},{thirdValue.DisposeCount}");
                                 composition.Dispose();
                             }

                             private static void DisposeOnlyMiddleFactoryHandle()
                             {
                                 var composition = new Composition();
                                 Func<Owned<IService>> factory = composition.ServiceFactory;
                                 Owned<IService> first = factory();
                                 Owned<IService> second = factory();
                                 Owned<IService> third = factory();
                                 IService firstValue = first.Value;
                                 IService secondValue = second.Value;
                                 IService thirdValue = third.Value;

                                 second.Dispose();
                                 Console.WriteLine($"after-middle={firstValue.DisposeCount},{secondValue.DisposeCount},{thirdValue.DisposeCount}");
                                 first.Dispose();
                                 third.Dispose();
                                 Console.WriteLine($"after-cleanup={firstValue.DisposeCount},{secondValue.DisposeCount},{thirdValue.DisposeCount}");
                                 composition.Dispose();
                             }

                             private static void DisposeNestedInnerHandle()
                             {
                                 var composition = new Composition();
                                 Owned<IOuter> outer = composition.OwnedOuter;
                                 IOuter outerValue = outer.Value;
                                 Owned<IInner> inner = outerValue.Inner;
                                 IInner innerValue = inner.Value;

                                 inner.Dispose();
                                 Console.WriteLine($"after-inner={innerValue.DisposeCount},{outerValue.DisposeCount}");
                                 outer.Dispose();
                                 Console.WriteLine($"after-outer={innerValue.DisposeCount},{outerValue.DisposeCount}");
                                 composition.Dispose();
                             }

                             private static void DisposeNestedOuterHandle()
                             {
                                 var composition = new Composition();
                                 Owned<IOuter> outer = composition.OwnedOuter;
                                 IOuter outerValue = outer.Value;
                                 Owned<IInner> inner = outerValue.Inner;
                                 IInner innerValue = inner.Value;

                                 outer.Dispose();
                                 Console.WriteLine($"after-outer={innerValue.DisposeCount},{outerValue.DisposeCount}");
                                 inner.Dispose();
                                 Console.WriteLine($"after-inner-cleanup={innerValue.DisposeCount},{outerValue.DisposeCount}");
                                 composition.Dispose();
                             }

                             private static void DisposeDiamondGraph()
                             {
                                 var composition = new Composition();
                                 Owned<IConsumer> owned = composition.OwnedConsumer;
                                 IConsumer consumer = owned.Value;
                                 ILeft left = consumer.Left;
                                 IRight right = consumer.Right;
                                 IShared shared = left.Shared;

                                 Console.WriteLine("shared=" + ReferenceEquals(shared, right.Shared));
                                 owned.Dispose();
                                 Console.WriteLine($"dispose-counts={consumer.DisposeCount},{left.DisposeCount},{right.DisposeCount},{shared.DisposeCount}");
                                 Console.WriteLine("dispose-order=" + string.Join(",", Tracker.DisposalOrder));
                                 composition.Dispose();
                             }

                             private static void DisposeMiddleTripleNestedHandle()
                             {
                                 var composition = new Composition();
                                 Owned<Owned<Owned<IService>>> outer = composition.TripleOwned;
                                 Owned<Owned<IService>> middle = outer.Value;
                                 Owned<IService> inner = middle.Value;
                                 IService value = inner.Value;

                                 middle.Dispose();
                                 Console.WriteLine("after-middle=" + value.DisposeCount);
                                 outer.Dispose();
                                 Console.WriteLine("after-outer=" + value.DisposeCount);
                                 inner.Dispose();
                                 Console.WriteLine("after-inner=" + value.DisposeCount);
                                 composition.Dispose();
                             }

                             private static void IsolateFactoryUnitsWithNestedHandles()
                             {
                                 var composition = new Composition();
                                 Func<Owned<IOuter>> factory = composition.OuterFactory;
                                 Owned<IOuter> first = factory();
                                 Owned<IOuter> second = factory();
                                 IOuter firstOuter = first.Value;
                                 IOuter secondOuter = second.Value;
                                 Owned<IInner> firstInner = firstOuter.Inner;
                                 Owned<IInner> secondInner = secondOuter.Inner;
                                 IInner firstInnerValue = firstInner.Value;
                                 IInner secondInnerValue = secondInner.Value;

                                 firstInner.Dispose();
                                 Console.WriteLine($"after-first-inner={firstInnerValue.DisposeCount},{firstOuter.DisposeCount},{secondInnerValue.DisposeCount},{secondOuter.DisposeCount}");
                                 first.Dispose();
                                 Console.WriteLine($"after-first-outer={firstInnerValue.DisposeCount},{firstOuter.DisposeCount},{secondInnerValue.DisposeCount},{secondOuter.DisposeCount}");
                                 second.Dispose();
                                 Console.WriteLine($"after-second-outer={firstInnerValue.DisposeCount},{firstOuter.DisposeCount},{secondInnerValue.DisposeCount},{secondOuter.DisposeCount}");
                                 secondInner.Dispose();
                                 Console.WriteLine($"after-cleanup={firstInnerValue.DisposeCount},{firstOuter.DisposeCount},{secondInnerValue.DisposeCount},{secondOuter.DisposeCount}");
                                 composition.Dispose();
                             }

                             private static void DisposeMixedLifetimeGraph()
                             {
                                 var composition = new Composition();
                                 Owned<IApp> owned = composition.OwnedApp;
                                 IApp app = owned.Value;
                                 Owned<IJob> nestedJob = app.OwnedJob;
                                 IJob nestedJobValue = nestedJob.Value;
                                 Owned<IJob> extraJob = app.JobFactory();
                                 IJob extraJobValue = extraJob.Value;

                                 extraJob.Dispose();
                                 Console.WriteLine($"after-extra={app.DisposeCount},{app.Worker.DisposeCount},{nestedJobValue.DisposeCount},{extraJobValue.DisposeCount},{app.Log.DisposeCount}");
                                 owned.Dispose();
                                 Console.WriteLine($"after-app={app.DisposeCount},{app.Worker.DisposeCount},{nestedJobValue.DisposeCount},{extraJobValue.DisposeCount},{app.Log.DisposeCount}");
                                 nestedJob.Dispose();
                                 Console.WriteLine($"after-nested={app.DisposeCount},{app.Worker.DisposeCount},{nestedJobValue.DisposeCount},{extraJobValue.DisposeCount},{app.Log.DisposeCount}");
                                 composition.Dispose();
                                 Console.WriteLine($"after-container={app.DisposeCount},{app.Worker.DisposeCount},{nestedJobValue.DisposeCount},{extraJobValue.DisposeCount},{app.Log.DisposeCount}");
                             }

                             private static bool Distinct(params object[] values) =>
                                 values.Distinct(ReferenceEqualityComparer.Instance).Count() == values.Length;
                         }

                         internal static class Tracker
                         {
                             public static List<string> DisposalOrder { get; } = new List<string>();

                             public static void Disposed(string kind) => DisposalOrder.Add(kind);
                         }

                         abstract class Tracked : IDisposable
                         {
                             private readonly string kind;

                             protected Tracked(string kind) => this.kind = kind;

                             public int DisposeCount { get; private set; }

                             public void Dispose()
                             {
                                 DisposeCount++;
                                 Tracker.Disposed(kind);
                             }
                         }

                         interface IService
                         {
                             int DisposeCount { get; }
                         }

                         sealed class Service : Tracked, IService
                         {
                             public Service() : base("Service") { }
                         }

                         interface IInner
                         {
                             int DisposeCount { get; }
                         }

                         sealed class Inner : Tracked, IInner
                         {
                             public Inner() : base("Inner") { }
                         }

                         interface IOuter
                         {
                             Owned<IInner> Inner { get; }

                             int DisposeCount { get; }
                         }

                         sealed class Outer : Tracked, IOuter
                         {
                             public Outer(Owned<IInner> inner) : base("Outer") => Inner = inner;

                             public Owned<IInner> Inner { get; }
                         }

                         interface IShared
                         {
                             int DisposeCount { get; }
                         }

                         sealed class Shared : Tracked, IShared
                         {
                             public Shared() : base("Shared") { }
                         }

                         interface ILeft
                         {
                             IShared Shared { get; }

                             int DisposeCount { get; }
                         }

                         sealed class Left : Tracked, ILeft
                         {
                             public Left(IShared shared) : base("Left") => Shared = shared;

                             public IShared Shared { get; }
                         }

                         interface IRight
                         {
                             IShared Shared { get; }

                             int DisposeCount { get; }
                         }

                         sealed class Right : Tracked, IRight
                         {
                             public Right(IShared shared) : base("Right") => Shared = shared;

                             public IShared Shared { get; }
                         }

                         interface IConsumer
                         {
                             ILeft Left { get; }

                             IRight Right { get; }

                             int DisposeCount { get; }
                         }

                         sealed class Consumer : Tracked, IConsumer
                         {
                             public Consumer(ILeft left, IRight right) : base("Consumer")
                             {
                                 Left = left;
                                 Right = right;
                             }

                             public ILeft Left { get; }

                             public IRight Right { get; }
                         }

                         interface ISingletonLog
                         {
                             int DisposeCount { get; }
                         }

                         sealed class SingletonLog : Tracked, ISingletonLog
                         {
                             public SingletonLog() : base("SingletonLog") { }
                         }

                         interface IWorker
                         {
                             int DisposeCount { get; }
                         }

                         sealed class Worker : Tracked, IWorker
                         {
                             public Worker() : base("Worker") { }
                         }

                         interface IJob
                         {
                             int DisposeCount { get; }
                         }

                         sealed class Job : Tracked, IJob
                         {
                             public Job() : base("Job") { }
                         }

                         interface IApp
                         {
                             ISingletonLog Log { get; }

                             IWorker Worker { get; }

                             Owned<IJob> OwnedJob { get; }

                             Func<Owned<IJob>> JobFactory { get; }

                             int DisposeCount { get; }
                         }

                         sealed class App : Tracked, IApp
                         {
                             public App(
                                 ISingletonLog log,
                                 IWorker worker,
                                 Owned<IJob> ownedJob,
                                 Func<Owned<IJob>> jobFactory)
                                 : base("App")
                             {
                                 Log = log;
                                 Worker = worker;
                                 OwnedJob = ownedJob;
                                 JobFactory = jobFactory;
                             }

                             public ISingletonLog Log { get; }

                             public IWorker Worker { get; }

                             public Owned<IJob> OwnedJob { get; }

                             public Func<Owned<IJob>> JobFactory { get; }
                         }

                         partial class Composition
                         {
                             private static void Setup() =>
                                 DI.Setup(nameof(Composition))
                                     .Bind<IService>().As(Transient).To<Service>()
                                     .Bind<IInner>().As(Transient).To<Inner>()
                                     .Bind<IOuter>().As(Transient).To<Outer>()
                                     .Bind<IShared>().As(PerResolve).To<Shared>()
                                     .Bind<ILeft>().As(Transient).To<Left>()
                                     .Bind<IRight>().As(Transient).To<Right>()
                                     .Bind<IConsumer>().As(Transient).To<Consumer>()
                                     .Bind<ISingletonLog>().As(Singleton).To<SingletonLog>()
                                     .Bind<IWorker>().As(Transient).To<Worker>()
                                     .Bind<IJob>().As(Transient).To<Job>()
                                     .Bind<IApp>().As(Transient).To<App>()
                                     .Root<Func<Owned<IService>>>("ServiceFactory")
                                     .Root<Owned<IOuter>>("OwnedOuter")
                                     .Root<Owned<IConsumer>>("OwnedConsumer")
                                     .Root<Owned<Owned<Owned<IService>>>>("TripleOwned")
                                     .Root<Func<Owned<IOuter>>>("OuterFactory")
                                     .Root<Owned<IApp>>("OwnedApp");
                         }
                     }
                     """.Replace("#scenario#", ((int)scenario).ToString(CultureInfo.InvariantCulture));

        var result = await source.RunAsync(new Options(LanguageVersion.CSharp10));
        result.Success.ShouldBeTrue(result);
        return result.StdOut;
    }
}
