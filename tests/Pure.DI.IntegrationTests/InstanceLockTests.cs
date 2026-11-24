namespace Pure.DI.IntegrationTests;

/// <summary>
/// A single instance is created under a lock of its own, so creating it waits only for the instances it depends on.
/// Compositions that create the singletons of each other on several threads do not deadlock, see #206.
/// </summary>
public class InstanceLockTests
{
    [Fact]
    public async Task ShouldNotDeadlockWhenCompositionsCreateSingletonsOfEachOther()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               interface IClock { }

                               interface IBack { }

                               interface IFront { }

                               // The root of Module is generated in the same compilation, so the factory of App reads it by this interface.
                               interface IBackProvider
                               {
                                   IBack Back { get; }
                               }

                               static class Rendezvous
                               {
                                   private static int _count;

                                   // Waits for the other thread up to a second, so both threads are inside their constructors.
                                   public static void Meet()
                                   {
                                       Interlocked.Increment(ref _count);
                                       var start = DateTime.UtcNow;
                                       while (Volatile.Read(ref _count) < 2 && DateTime.UtcNow - start < TimeSpan.FromSeconds(1))
                                       {
                                           Thread.Sleep(1);
                                       }
                                   }
                               }

                               sealed class Clock : IClock { }

                               sealed class Back : IBack
                               {
                                   public Back(Func<IClock> clock)
                                   {
                                       Rendezvous.Meet();
                                       clock();
                                   }
                               }

                               sealed class Front : IFront
                               {
                                   public Front(Func<IBack> back)
                                   {
                                       Rendezvous.Meet();
                                       back();
                                   }
                               }

                               partial class Module : IBackProvider
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Module))
                                           .Arg<Func<IClock>>("clock")
                                           .Bind<IBack>().As(Singleton).To<Back>()
                                           .Root<IBack>("Back");
                               }

                               static class ModuleFactory
                               {
                                   public static Module Create(Func<IClock> clock) => new Module(clock);
                               }

                               partial class App
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(App))
                                           .Bind<IClock>().As(Singleton).To<Clock>()
                                           .Bind<IFront>().As(Singleton).To<Front>()
                                           .Bind().As(Singleton).To(ctx =>
                                           {
                                               ctx.Inject(out Func<IClock> clock);
                                               return ModuleFactory.Create(clock);
                                           })
                                           .Bind<IBack>().To(ctx =>
                                           {
                                               ctx.Inject(out IBackProvider module);
                                               return module.Back;
                                           })
                                           .Root<Module>("Module")
                                           .Root<IFront>("Front");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var app = new App();
                                       var module = app.Module;
                                       var front = Task.Run(() => app.Front);
                                       var back = Task.Run(() => module.Back);
                                       Console.WriteLine(Task.WaitAll(new Task[] { front, back }, TimeSpan.FromSeconds(10)));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    public async Task ShouldCreateIndependentInstancesAtTheSameTime(string lifetime)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               static class Rendezvous
                               {
                                   private static int _count;

                                   // Returns true when the other thread comes in 10 seconds.
                                   public static bool Meet()
                                   {
                                       Interlocked.Increment(ref _count);
                                       var start = DateTime.UtcNow;
                                       while (Volatile.Read(ref _count) < 2)
                                       {
                                           if (DateTime.UtcNow - start > TimeSpan.FromSeconds(10))
                                           {
                                               return false;
                                           }

                                           Thread.Sleep(1);
                                       }

                                       return true;
                                   }
                               }

                               class First
                               {
                                   public bool IsMet { get; } = Rendezvous.Meet();
                               }

                               class Second
                               {
                                   public bool IsMet { get; } = Rendezvous.Meet();
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.#lifetime#).To<First>()
                                           .Bind().As(Lifetime.#lifetime#).To<Second>()
                                           .Root<First>("First")
                                           .Root<Second>("Second");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var first = Task.Run(() => composition.First);
                                       var second = Task.Run(() => composition.Second);
                                       Console.WriteLine(first.Result.IsMet && second.Result.IsMet);
                                       Console.WriteLine(ReferenceEquals(first.Result, composition.First) && ReferenceEquals(second.Result, composition.Second));
                                   }
                               }
                           }
                           """.Replace("#lifetime#", lifetime).RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
        result.GeneratedCode.ShouldNotContain("lock (_lock", Case.Sensitive, result);
    }

    [Fact]
    public async Task ShouldCreateSingletonOnceWhenManyThreadsNeedIt()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Linq;
                           using System.Threading;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Service
                               {
                                   public static int Count;

                                   public Service()
                                   {
                                       Interlocked.Increment(ref Count);
                                       Thread.Sleep(50);
                                   }
                               }

                               class Consumer(Service service)
                               {
                                   public Service Service { get; } = service;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Service>()
                                           .Root<Consumer>("Consumer");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var consumers = Enumerable.Range(0, 8)
                                           .Select(_ => Task.Run(() => composition.Consumer))
                                           .ToArray();
                                       Task.WaitAll(consumers);
                                       Console.WriteLine(Service.Count);
                                       Console.WriteLine(consumers.All(i => ReferenceEquals(i.Result.Service, consumers[0].Result.Service)));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "True"], result);
    }

    [Fact]
    public async Task ShouldCreatePerResolveInstanceOnceWhenLambdasRunOnManyThreads()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Linq;
                           using System.Threading;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Service
                               {
                                   public Service() => Thread.Sleep(50);
                               }

                               class Consumer(Func<Service> factory)
                               {
                                   public Service[] Services { get; } = Enumerable.Range(0, 8)
                                       .Select(_ => Task.Run(factory))
                                       .ToArray()
                                       .Select(i => i.Result)
                                       .ToArray();
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.PerResolve).To<Service>()
                                           .Root<Consumer>("Consumer");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var services = new Composition().Consumer.Services;
                                       Console.WriteLine(services.All(i => ReferenceEquals(i, services[0])));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldUseLockOfDelegateAsContextLockOfTransientFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Linq;
                           using System.Threading;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               class Counter
                               {
                                   private int _current;
                                   private int _max;

                                   public int Max => _max;

                                   public void Enter()
                                   {
                                       var current = Interlocked.Increment(ref _current);
                                       if (current > _max) _max = current;
                                       Thread.Sleep(10);
                                       Interlocked.Decrement(ref _current);
                                   }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Counter>()
                                           .Bind<Func<int, int>>().To<Func<int, int>>(ctx => value =>
                                           {
                                               ctx.Inject(out Counter counter);
                                               lock (ctx.Lock)
                                               {
                                                   counter.Enter();
                                                   return value;
                                               }
                                           })
                                           .Root<Func<int, int>>("Factory")
                                           .Root<Counter>("Counter");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Task.WaitAll(Enumerable.Range(0, 8).Select(i => Task.Run(() => factory(i))).ToArray());
                                       Console.WriteLine(composition.Counter.Max);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1"], result);
    }
}
