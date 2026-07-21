namespace Pure.DI.IntegrationTests;

public class OwnedTests
{
    [Fact]
    public async Task ShouldDisposeAsyncOnlyInstanceAsynchronously()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main() => Run().GetAwaiter().GetResult();
                           
                                   private static async Task Run()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IAsyncOnly> owned = composition.AsyncOnlyRoot;
                                       IAsyncOnly service = owned.Value;
                           
                                       Console.WriteLine(!(service.AsyncDisposed));
                                       await owned.DisposeAsync();
                                       Console.WriteLine(service.AsyncDisposed);
                                   }
                           
                                   private const string ExpectedSyncOverAsync = "threw=False; asyncDisposed=True";
                           
                                   private static string DescribeSyncOverAsync(Exception? thrown, IAsyncOnly service) =>
                                       $"threw={thrown is not null}; asyncDisposed={service.AsyncDisposed}";
                           
                                   private static string DescribeMixed(IMixed service) =>
                                       $"sync={service.SyncDisposed}; async={service.AsyncDisposed}";
                           
                               }
                           
                               interface IAsyncOnly
                               {
                                   bool AsyncDisposed { get; }
                               }
                           
                               sealed class AsyncOnly : IAsyncOnly, IAsyncDisposable
                               {
                                   public bool AsyncDisposed { get; private set; }
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       AsyncDisposed = true;
                                       return default;
                                   }
                               }
                           
                               interface IMixed
                               {
                                   bool SyncDisposed { get; }
                           
                                   bool AsyncDisposed { get; }
                               }
                           
                               sealed class Mixed : IMixed, IDisposable, IAsyncDisposable
                               {
                                   public bool SyncDisposed { get; private set; }
                           
                                   public bool AsyncDisposed { get; private set; }
                           
                                   public void Dispose() => SyncDisposed = true;
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       AsyncDisposed = true;
                                       return default;
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IAsyncOnly>().To<AsyncOnly>()
                                           .Bind<IMixed>().To<Mixed>()
                                           .Root<Owned<IAsyncOnly>>("AsyncOnlyRoot")
                                           .Root<Owned<IMixed>>("MixedRoot");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeAsyncOnlyInstanceSynchronously()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IAsyncOnly> owned = composition.AsyncOnlyRoot;
                                       IAsyncOnly service = owned.Value;
                           
                                       Exception? thrown = Test.RecordException(() => owned.Dispose());
                                       Console.WriteLine(Test.AreEqual(DescribeSyncOverAsync(thrown, service), ExpectedSyncOverAsync));
                                   }
                           
                                   private const string ExpectedSyncOverAsync = "threw=False; asyncDisposed=True";
                           
                                   private static string DescribeSyncOverAsync(Exception? thrown, IAsyncOnly service) =>
                                       $"threw={thrown is not null}; asyncDisposed={service.AsyncDisposed}";
                           
                                   private static string DescribeMixed(IMixed service) =>
                                       $"sync={service.SyncDisposed}; async={service.AsyncDisposed}";
                           
                               }
                           
                               interface IAsyncOnly
                               {
                                   bool AsyncDisposed { get; }
                               }
                           
                               sealed class AsyncOnly : IAsyncOnly, IAsyncDisposable
                               {
                                   public bool AsyncDisposed { get; private set; }
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       AsyncDisposed = true;
                                       return default;
                                   }
                               }
                           
                               interface IMixed
                               {
                                   bool SyncDisposed { get; }
                           
                                   bool AsyncDisposed { get; }
                               }
                           
                               sealed class Mixed : IMixed, IDisposable, IAsyncDisposable
                               {
                                   public bool SyncDisposed { get; private set; }
                           
                                   public bool AsyncDisposed { get; private set; }
                           
                                   public void Dispose() => SyncDisposed = true;
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       AsyncDisposed = true;
                                       return default;
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IAsyncOnly>().To<AsyncOnly>()
                                           .Bind<IMixed>().To<Mixed>()
                                           .Root<Owned<IAsyncOnly>>("AsyncOnlyRoot")
                                           .Root<Owned<IMixed>>("MixedRoot");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeMixedInstanceSynchronouslyOnly()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IMixed> owned = composition.MixedRoot;
                                       IMixed service = owned.Value;
                           
                                       owned.Dispose();
                                       Console.WriteLine(Test.AreEqual(DescribeMixed(service), "sync=True; async=False"));
                                   }
                           
                                   private const string ExpectedSyncOverAsync = "threw=False; asyncDisposed=True";
                           
                                   private static string DescribeSyncOverAsync(Exception? thrown, IAsyncOnly service) =>
                                       $"threw={thrown is not null}; asyncDisposed={service.AsyncDisposed}";
                           
                                   private static string DescribeMixed(IMixed service) =>
                                       $"sync={service.SyncDisposed}; async={service.AsyncDisposed}";
                           
                               }
                           
                               interface IAsyncOnly
                               {
                                   bool AsyncDisposed { get; }
                               }
                           
                               sealed class AsyncOnly : IAsyncOnly, IAsyncDisposable
                               {
                                   public bool AsyncDisposed { get; private set; }
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       AsyncDisposed = true;
                                       return default;
                                   }
                               }
                           
                               interface IMixed
                               {
                                   bool SyncDisposed { get; }
                           
                                   bool AsyncDisposed { get; }
                               }
                           
                               sealed class Mixed : IMixed, IDisposable, IAsyncDisposable
                               {
                                   public bool SyncDisposed { get; private set; }
                           
                                   public bool AsyncDisposed { get; private set; }
                           
                                   public void Dispose() => SyncDisposed = true;
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       AsyncDisposed = true;
                                       return default;
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IAsyncOnly>().To<AsyncOnly>()
                                           .Bind<IMixed>().To<Mixed>()
                                           .Root<Owned<IAsyncOnly>>("AsyncOnlyRoot")
                                           .Root<Owned<IMixed>>("MixedRoot");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeMixedInstanceAsynchronouslyOnly()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main() => Run().GetAwaiter().GetResult();
                           
                                   private static async Task Run()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IMixed> owned = composition.MixedRoot;
                                       IMixed service = owned.Value;
                           
                                       await owned.DisposeAsync();
                                       Console.WriteLine(Test.AreEqual(DescribeMixed(service), "sync=False; async=True"));
                                   }
                           
                                   private const string ExpectedSyncOverAsync = "threw=False; asyncDisposed=True";
                           
                                   private static string DescribeSyncOverAsync(Exception? thrown, IAsyncOnly service) =>
                                       $"threw={thrown is not null}; asyncDisposed={service.AsyncDisposed}";
                           
                                   private static string DescribeMixed(IMixed service) =>
                                       $"sync={service.SyncDisposed}; async={service.AsyncDisposed}";
                           
                               }
                           
                               interface IAsyncOnly
                               {
                                   bool AsyncDisposed { get; }
                               }
                           
                               sealed class AsyncOnly : IAsyncOnly, IAsyncDisposable
                               {
                                   public bool AsyncDisposed { get; private set; }
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       AsyncDisposed = true;
                                       return default;
                                   }
                               }
                           
                               interface IMixed
                               {
                                   bool SyncDisposed { get; }
                           
                                   bool AsyncDisposed { get; }
                               }
                           
                               sealed class Mixed : IMixed, IDisposable, IAsyncDisposable
                               {
                                   public bool SyncDisposed { get; private set; }
                           
                                   public bool AsyncDisposed { get; private set; }
                           
                                   public void Dispose() => SyncDisposed = true;
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       AsyncDisposed = true;
                                       return default;
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IAsyncOnly>().To<AsyncOnly>()
                                           .Bind<IMixed>().To<Mixed>()
                                           .Root<Owned<IAsyncOnly>>("AsyncOnlyRoot")
                                           .Root<Owned<IMixed>>("MixedRoot");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSuppressOwnedDisposalExceptionAndContinueDisposing()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IRoot> owned = composition.Root;
                                       IRoot root = owned.Value;
                           
                                       // This behavior is intentionally not changed. The .NET dispose pattern
                                       // recommends that Dispose does not throw, so an exception from a user's
                                       // dependency violates the expected disposal contract. Owned suppresses
                                       // that exception to ensure every other owned resource is still disposed.
                                       Exception? thrown = Test.RecordException(() => owned.Dispose());
                           
                                       Console.WriteLine(Test.AreEqual(DescribeState(thrown, root), ExpectedState));
                                   }
                           
                                   private const string ExpectedState =
                                       "thrown=False; before=True; attempted=True; after=True";
                           
                                   private static string DescribeState(Exception? thrown, IRoot root) =>
                                       $"thrown={thrown is not null}; " +
                                       $"before={root.Before.IsDisposed}; " +
                                       $"attempted={root.Thrower.DisposeAttempted}; " +
                                       $"after={root.After.IsDisposed}";
                           
                               }
                           
                               interface IBefore
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Before : IBefore, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IThrower
                               {
                                   bool DisposeAttempted { get; }
                               }
                           
                               sealed class Thrower : IThrower, IDisposable
                               {
                                   public bool DisposeAttempted { get; private set; }
                           
                                   public void Dispose()
                                   {
                                       DisposeAttempted = true;
                                       throw new InvalidOperationException("boom");
                                   }
                               }
                           
                               interface IAfter
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class After : IAfter, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IRoot
                               {
                                   IBefore Before { get; }
                           
                                   IThrower Thrower { get; }
                           
                                   IAfter After { get; }
                               }
                           
                               sealed class Root : IRoot
                               {
                                   public Root(IBefore before, IThrower thrower, IAfter after)
                                   {
                                       Before = before;
                                       Thrower = thrower;
                                       After = after;
                                   }

                                   public IBefore Before { get; }

                                   public IThrower Thrower { get; }

                                   public IAfter After { get; }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Before>()
                                           .Bind().To<Thrower>()
                                           .Bind().To<After>()
                                           .Bind().To<Root>()
                                           .Root<Owned<IRoot>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeOwnedGraphInReverseConstructionOrder()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       DisposalRecorder recorder = new DisposalRecorder();
                                       Composition composition = new Composition(recorder);
                                       Owned<IA> owned = composition.Root;
                                       _ = owned.Value.B.C;
                           
                                       owned.Dispose();
                                       Console.WriteLine(Test.AreEqual(recorder.Names, new[] { "A", "B", "C" }));
                                   }
                           
                               }
                           
                               sealed class DisposalRecorder
                               {
                                   private readonly List<string> names = new();
                           
                                   public IReadOnlyList<string> Names => names;
                           
                                   public void Record(string name) => names.Add(name);
                               }
                           
                               interface IC
                               {
                               }

                               sealed class C : IC, IDisposable
                               {
                                   private readonly DisposalRecorder recorder;

                                   public C(DisposalRecorder recorder) => this.recorder = recorder;

                                   public void Dispose() => recorder.Record("C");
                               }
                           
                               interface IB
                               {
                                   IC C { get; }
                               }
                           
                               sealed class B : IB, IDisposable
                               {
                                   private readonly DisposalRecorder recorder;

                                   public B(IC c, DisposalRecorder recorder)
                                   {
                                       C = c;
                                       this.recorder = recorder;
                                   }

                                   public IC C { get; }
                           
                                   public void Dispose() => recorder.Record("B");
                               }
                           
                               interface IA
                               {
                                   IB B { get; }
                               }
                           
                               sealed class A : IA, IDisposable
                               {
                                   private readonly DisposalRecorder recorder;

                                   public A(IB b, DisposalRecorder recorder)
                                   {
                                       B = b;
                                       this.recorder = recorder;
                                   }

                                   public IB B { get; }
                           
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
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeOwnedSynchronouslyOnlyOnce()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IResource> owned = composition.Root;
                                       IResource resource = owned.Value;
                           
                                       owned.Dispose();
                                       owned.Dispose();
                           
                                       Console.WriteLine(Test.AreEqual(resource.DisposeCount, 1));
                                   }
                           
                               }
                           
                               interface IResource
                               {
                                   int DisposeCount { get; }
                               }
                           
                               sealed class Resource : IResource, IDisposable, IAsyncDisposable
                               {
                                   public int DisposeCount { get; private set; }
                           
                                   public void Dispose() => DisposeCount++;
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       DisposeCount++;
                                       return default;
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IResource>().To<Resource>()
                                           .Root<Owned<IResource>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeOwnedAsynchronouslyOnlyOnce()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main() => Run().GetAwaiter().GetResult();
                           
                                   private static async Task Run()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IResource> owned = composition.Root;
                                       IResource resource = owned.Value;
                           
                                       await owned.DisposeAsync();
                                       await owned.DisposeAsync();
                           
                                       Console.WriteLine(Test.AreEqual(resource.DisposeCount, 1));
                                   }
                           
                               }
                           
                               interface IResource
                               {
                                   int DisposeCount { get; }
                               }
                           
                               sealed class Resource : IResource, IDisposable, IAsyncDisposable
                               {
                                   public int DisposeCount { get; private set; }
                           
                                   public void Dispose() => DisposeCount++;
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       DisposeCount++;
                                       return default;
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IResource>().To<Resource>()
                                           .Root<Owned<IResource>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeOwnedOnlyOnceWhenDisposedSynchronouslyThenAsynchronously()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main() => Run().GetAwaiter().GetResult();
                           
                                   private static async Task Run()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IResource> owned = composition.Root;
                                       IResource resource = owned.Value;
                           
                                       owned.Dispose();
                                       await owned.DisposeAsync();
                           
                                       Console.WriteLine(Test.AreEqual(resource.DisposeCount, 1));
                                   }
                           
                               }
                           
                               interface IResource
                               {
                                   int DisposeCount { get; }
                               }
                           
                               sealed class Resource : IResource, IDisposable, IAsyncDisposable
                               {
                                   public int DisposeCount { get; private set; }
                           
                                   public void Dispose() => DisposeCount++;
                           
                                   public ValueTask DisposeAsync()
                                   {
                                       DisposeCount++;
                                       return default;
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IResource>().To<Resource>()
                                           .Root<Owned<IResource>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeEveryFactoryCreatedWidget()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IRoot> owned = composition.Root;
                                       IReadOnlyList<IWidget> widgets = owned.Value.Widgets;
                           
                                       Console.WriteLine(Test.AreEqual(widgets.Count, 3));
                                       Console.WriteLine((widgets).All(widget => !widget.IsDisposed));
                           
                                       owned.Dispose();
                                       Console.WriteLine((widgets).All(widget => widget.IsDisposed));
                                   }
                           
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
                                       Widgets = new[] { widgetFactory(), widgetFactory(), widgetFactory() };
                           
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
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldCreateDistinctWidgetInstancesUsingFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IRoot> owned = composition.Root;
                                       IReadOnlyList<IWidget> widgets = owned.Value.Widgets;
                           
                                       Console.WriteLine(Test.AreEqual(widgets.Distinct().Count(), 3));
                                   }
                           
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
                                       Widgets = new[] { widgetFactory(), widgetFactory(), widgetFactory() };
                           
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
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeEveryMessagePumpUnitOfWork()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       IMessagePump pump = composition.Pump;
                           
                                       pump.Process(3);
                           
                                       Console.WriteLine(Test.AreEqual(pump.Handled.Count, 3));
                                       Console.WriteLine((pump.Handled).All(handler => handler.Connection.IsDisposed));
                                       Console.WriteLine(Test.AreEqual(pump.Handled.Select(handler => handler.Connection).Distinct().Count(), 3));
                                   }
                           
                               }
                           
                               interface IConnection
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Connection : IConnection, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IHandler
                               {
                                   IConnection Connection { get; }
                           
                                   void Handle();
                               }
                           
                               sealed class Handler : IHandler
                               {
                                   public Handler(IConnection connection) => Connection = connection;

                                   public IConnection Connection { get; }
                           
                                   public void Handle()
                                   {
                                   }
                               }
                           
                               interface IMessagePump
                               {
                                   IReadOnlyList<IHandler> Handled { get; }
                           
                                   void Process(int count);
                               }
                           
                               sealed class MessagePump : IMessagePump
                               {
                                   private readonly Func<Owned<IHandler>> handlerFactory;
                                   private readonly List<IHandler> handled = new();

                                   public MessagePump(Func<Owned<IHandler>> handlerFactory) =>
                                       this.handlerFactory = handlerFactory;
                           
                                   public IReadOnlyList<IHandler> Handled => handled;
                           
                                   public void Process(int count)
                                   {
                                       for (int i = 0; i < count; i++)
                                       {
                                           using Owned<IHandler> handler = handlerFactory();
                                           handler.Value.Handle();
                                           handled.Add(handler.Value);
                                       }
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Connection>()
                                           .Bind().To<Handler>()
                                           .Bind().To<MessagePump>()
                                           .Root<IMessagePump>("Pump");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldShareSingletonBetweenOuterAndInnerOwnedGraphs()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IOuter> owned = composition.Root;
                           
                                       Console.WriteLine(ReferenceEquals(owned.Value.Shared, owned.Value.Inner.Shared));
                                   }
                           
                               }
                           
                               interface IShared
                               {
                                   int DisposeCount { get; }
                               }
                           
                               sealed class Shared : IShared, IDisposable
                               {
                                   public int DisposeCount { get; private set; }
                           
                                   public void Dispose() => DisposeCount++;
                               }
                           
                               interface IInnerLocal
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class InnerLocal : IInnerLocal, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IOuterLocal
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class OuterLocal : IOuterLocal, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IInner
                               {
                                   IShared Shared { get; }
                           
                                   IInnerLocal Local { get; }
                               }
                           
                               sealed class Inner : IInner
                               {
                                   public Inner(IShared shared, IInnerLocal local)
                                   {
                                       Shared = shared;
                                       Local = local;
                                   }

                                   public IShared Shared { get; }

                                   public IInnerLocal Local { get; }
                               }
                           
                               interface IOuter
                               {
                                   IShared Shared { get; }
                           
                                   IOuterLocal Local { get; }
                           
                                   IInner Inner { get; }
                           
                                   void DisposeInner();
                               }
                           
                               sealed class Outer : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned;

                                   public Outer(Func<Owned<IInner>> innerFactory, IShared shared, IOuterLocal local)
                                   {
                                       innerOwned = innerFactory();
                                       Shared = shared;
                                       Local = local;
                                   }

                                   public IShared Shared { get; }

                                   public IOuterLocal Local { get; }
                           
                                   public IInner Inner => innerOwned.Value;
                           
                                   public void DisposeInner() => innerOwned.Dispose();
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Shared>()
                                           .Bind().To<InnerLocal>()
                                           .Bind().To<OuterLocal>()
                                           .Bind().To<Inner>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<IOuter>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotDisposeSingletonWithOuterOwned()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IOuter> owned = composition.Root;
                                       IShared shared = owned.Value.Shared;
                           
                                       owned.Dispose();
                                       Console.WriteLine(Test.AreEqual(shared.DisposeCount, 0));
                           
                                       IDisposable? disposableComposition = composition as IDisposable;
                                       Console.WriteLine(disposableComposition is not null);
                                       if (disposableComposition is null) return;
                                       disposableComposition.Dispose();
                                       Console.WriteLine(Test.AreEqual(shared.DisposeCount, 1));
                                   }
                           
                               }
                           
                               interface IShared
                               {
                                   int DisposeCount { get; }
                               }
                           
                               sealed class Shared : IShared, IDisposable
                               {
                                   public int DisposeCount { get; private set; }
                           
                                   public void Dispose() => DisposeCount++;
                               }
                           
                               interface IInnerLocal
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class InnerLocal : IInnerLocal, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IOuterLocal
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class OuterLocal : IOuterLocal, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IInner
                               {
                                   IShared Shared { get; }
                           
                                   IInnerLocal Local { get; }
                               }
                           
                               sealed class Inner : IInner
                               {
                                   public Inner(IShared shared, IInnerLocal local)
                                   {
                                       Shared = shared;
                                       Local = local;
                                   }

                                   public IShared Shared { get; }

                                   public IInnerLocal Local { get; }
                               }
                           
                               interface IOuter
                               {
                                   IShared Shared { get; }
                           
                                   IOuterLocal Local { get; }
                           
                                   IInner Inner { get; }
                           
                                   void DisposeInner();
                               }
                           
                               sealed class Outer : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned;

                                   public Outer(Func<Owned<IInner>> innerFactory, IShared shared, IOuterLocal local)
                                   {
                                       innerOwned = innerFactory();
                                       Shared = shared;
                                       Local = local;
                                   }

                                   public IShared Shared { get; }

                                   public IOuterLocal Local { get; }
                           
                                   public IInner Inner => innerOwned.Value;
                           
                                   public void DisposeInner() => innerOwned.Dispose();
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Shared>()
                                           .Bind().To<InnerLocal>()
                                           .Bind().To<OuterLocal>()
                                           .Bind().To<Inner>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<IOuter>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldNotDisposeSingletonWithInnerOwned()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IOuter> owned = composition.Root;
                                       IShared shared = owned.Value.Shared;
                           
                                       owned.Value.DisposeInner();
                                       Console.WriteLine(Test.AreEqual(shared.DisposeCount, 0));
                                   }
                           
                               }
                           
                               interface IShared
                               {
                                   int DisposeCount { get; }
                               }
                           
                               sealed class Shared : IShared, IDisposable
                               {
                                   public int DisposeCount { get; private set; }
                           
                                   public void Dispose() => DisposeCount++;
                               }
                           
                               interface IInnerLocal
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class InnerLocal : IInnerLocal, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IOuterLocal
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class OuterLocal : IOuterLocal, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IInner
                               {
                                   IShared Shared { get; }
                           
                                   IInnerLocal Local { get; }
                               }
                           
                               sealed class Inner : IInner
                               {
                                   public Inner(IShared shared, IInnerLocal local)
                                   {
                                       Shared = shared;
                                       Local = local;
                                   }

                                   public IShared Shared { get; }

                                   public IInnerLocal Local { get; }
                               }
                           
                               interface IOuter
                               {
                                   IShared Shared { get; }
                           
                                   IOuterLocal Local { get; }
                           
                                   IInner Inner { get; }
                           
                                   void DisposeInner();
                               }
                           
                               sealed class Outer : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned;

                                   public Outer(Func<Owned<IInner>> innerFactory, IShared shared, IOuterLocal local)
                                   {
                                       innerOwned = innerFactory();
                                       Shared = shared;
                                       Local = local;
                                   }

                                   public IShared Shared { get; }

                                   public IOuterLocal Local { get; }
                           
                                   public IInner Inner => innerOwned.Value;
                           
                                   public void DisposeInner() => innerOwned.Dispose();
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Shared>()
                                           .Bind().To<InnerLocal>()
                                           .Bind().To<OuterLocal>()
                                           .Bind().To<Inner>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<IOuter>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("// ThreadSafe = Off")]
    public async Task ShouldKeepOuterAliveAfterDirectlyInjectedNestedOwnedDisposal(string threadSafeHint)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Owned<IOuter> outer = composition.Root;
                                       IOuter outerValue = outer.Value;
                                       Owned<IInner> inner = outerValue.Inner;

                                       inner.Dispose();

                                       Console.WriteLine(inner.Value.IsDisposed);
                                       Console.WriteLine(!outerValue.IsDisposed);

                                       Owned<IOuter> secondOuter = composition.Root;
                                       Owned<IInner> secondInner = secondOuter.Value.Inner;

                                       secondOuter.Dispose();

                                       Console.WriteLine(secondOuter.Value.IsDisposed);
                                       Console.WriteLine(!secondInner.Value.IsDisposed);
                                       secondInner.Dispose();
                                       Console.WriteLine(secondInner.Value.IsDisposed);
                                   }
                               }

                               interface IInner
                               {
                                   bool IsDisposed { get; }
                               }

                               sealed class Inner : IInner, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               interface IOuter
                               {
                                   bool IsDisposed { get; }

                                   Owned<IInner> Inner { get; }
                               }

                               sealed class Outer : IOuter, IDisposable
                               {
                                   public Outer(Owned<IInner> inner) => Inner = inner;

                                   public bool IsDisposed { get; private set; }

                                   public Owned<IInner> Inner { get; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       #threadSafeHint#
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Inner>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<IOuter>>("Root");
                               }
                           }
                           """
            .Replace("#threadSafeHint#", threadSafeHint)
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldKeepEveryDirectlyNestedOwnedLevelIndependent()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Owned<Owned<Owned<Resource>>> outer = composition.Root;
                                       Owned<Owned<Resource>> middle = outer.Value;
                                       Owned<Resource> inner = middle.Value;

                                       outer.Dispose();
                                       Console.WriteLine(!inner.Value.IsDisposed);

                                       middle.Dispose();
                                       Console.WriteLine(!inner.Value.IsDisposed);

                                       inner.Dispose();
                                       Console.WriteLine(inner.Value.IsDisposed);
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Owned<Owned<Owned<Resource>>>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldIsolateDirectlyNestedUserDefinedOwnershipGraphs()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       CustomOwned<Outer> outer = composition.Root;
                                       CustomOwned<Inner> inner = outer.Value.Inner;

                                       inner.Dispose();

                                       Console.WriteLine(inner.Value.IsDisposed);
                                       Console.WriteLine(!outer.Value.IsDisposed);
                                   }
                               }

                               interface ICustomOwned : IDisposable
                               {
                               }

                               sealed class CustomAccumulator : List<object>, ICustomOwned
                               {
                                   public void Dispose()
                                   {
                                       for (var i = Count - 1; i >= 0; i--)
                                       {
                                           if (this[i] is IDisposable disposable and not ICustomOwned)
                                           {
                                               disposable.Dispose();
                                           }
                                       }
                                   }
                               }

                               readonly struct CustomOwned<T> : ICustomOwned
                               {
                                   private readonly ICustomOwned owner;

                                   public CustomOwned(T value, ICustomOwned owner)
                                   {
                                       Value = value;
                                       this.owner = owner;
                                   }

                                   public T Value { get; }

                                   public void Dispose() => owner.Dispose();
                               }

                               sealed class Inner : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Outer : IDisposable
                               {
                                   public Outer(CustomOwned<Inner> inner) => Inner = inner;

                                   public CustomOwned<Inner> Inner { get; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Accumulate<IDisposable, CustomAccumulator>(Transient, PerResolve, PerBlock)
                                           .Bind<ICustomOwned>().To((CustomAccumulator accumulator) => accumulator)
                                           .Bind<CustomOwned<TT>>().As(PerBlock).To(ctx =>
                                           {
                                               ctx.Inject<ICustomOwned>(out var owner);
                                               ctx.Inject<TT>(ctx.Tag, out var value);
                                               return new CustomOwned<TT>(value, owner);
                                           })
                                           .Bind().To<Inner>()
                                           .Bind().To<Outer>()
                                           .Root<CustomOwned<Outer>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldKeepFactoryOwnedUnitsAndTheirDirectlyNestedOwnedGraphsIndependent()
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
                               public static class Program
                               {
                                   public static async Task Main()
                                   {
                                       var composition = new Composition();
                                       Func<Owned<Outer>> factory = composition.Factory;
                                       Task<Owned<Outer>> firstTask = Task.Run(factory);
                                       Task<Owned<Outer>> secondTask = Task.Run(factory);
                                       await Task.WhenAll(firstTask, secondTask);
                                       Owned<Outer> first = firstTask.Result;
                                       Owned<Outer> second = secondTask.Result;

                                       first.Value.Inner.Dispose();

                                       Console.WriteLine(first.Value.Inner.Value.IsDisposed);
                                       Console.WriteLine(!first.Value.IsDisposed);
                                       Console.WriteLine(!second.Value.Inner.Value.IsDisposed);
                                       Console.WriteLine(!second.Value.IsDisposed);
                                   }
                               }

                               sealed class Inner : IDisposable
                               {
                                   private static readonly object Sync = new object();
                                   private static int creatingCount;

                                   public Inner()
                                   {
                                       lock (Sync)
                                       {
                                           if (++creatingCount < 2)
                                           {
                                               Monitor.Wait(Sync);
                                           }
                                           else
                                           {
                                               Monitor.PulseAll(Sync);
                                           }
                                       }
                                   }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Outer : IDisposable
                               {
                                   public Outer(Owned<Inner> inner) => Inner = inner;

                                   public Owned<Inner> Inner { get; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Inner>()
                                           .Bind().To<Outer>()
                                           .Root<Func<Owned<Outer>>>("Factory");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldKeepMixedLifetimeNestedOwnedGraphIndependent()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Owned<App> app = composition.Root;
                                       Owned<Job> job = app.Value.Job;

                                       app.Dispose();

                                       Console.WriteLine(app.Value.IsDisposed);
                                       Console.WriteLine(app.Value.Worker.IsDisposed);
                                       Console.WriteLine(!app.Value.Shared.IsDisposed);
                                       Console.WriteLine(!job.Value.IsDisposed);

                                       job.Dispose();
                                       Console.WriteLine(job.Value.IsDisposed);
                                       composition.Dispose();
                                       Console.WriteLine(app.Value.Shared.IsDisposed);
                                   }
                               }

                               sealed class Shared : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Worker : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Job : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class App : IDisposable
                               {
                                   public App(Shared shared, Worker worker, Owned<Job> job)
                                   {
                                       Shared = shared;
                                       Worker = worker;
                                       Job = job;
                                   }

                                   public Shared Shared { get; }

                                   public Worker Worker { get; }

                                   public Owned<Job> Job { get; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Singleton).To<Shared>()
                                           .Bind().To<Worker>()
                                           .Bind().To<Job>()
                                           .Bind().To<App>()
                                           .Root<Owned<App>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldRollBackDirectlyNestedOwnedWhenOuterConstructionFails()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       try
                                       {
                                           _ = composition.Root;
                                       }
                                       catch (InvalidOperationException)
                                       {
                                       }

                                       Console.WriteLine(Inner.Last?.IsDisposed == true);
                                   }
                               }

                               sealed class Inner : IDisposable
                               {
                                   public Inner() => Last = this;

                                   public static Inner? Last { get; private set; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Outer
                               {
                                   public Outer(Owned<Inner> inner) =>
                                       throw new InvalidOperationException("Cannot create outer.");
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Inner>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<Outer>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldKeepSiblingDirectlyInjectedOwnedGraphsIndependent()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Owned<Outer> outer = composition.Root;
                                       Owned<Resource> first = outer.Value.First;
                                       Owned<Resource> second = outer.Value.Second;

                                       Console.WriteLine(!ReferenceEquals(first.Value, second.Value));

                                       first.Dispose();
                                       Console.WriteLine(first.Value.DisposeCount == 1);
                                       Console.WriteLine(second.Value.DisposeCount == 0);
                                       Console.WriteLine(outer.Value.DisposeCount == 0);

                                       outer.Dispose();
                                       Console.WriteLine(outer.Value.DisposeCount == 1);
                                       Console.WriteLine(second.Value.DisposeCount == 0);

                                       second.Dispose();
                                       Console.WriteLine(second.Value.DisposeCount == 1);
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   public int DisposeCount { get; private set; }

                                   public void Dispose() => DisposeCount++;
                               }

                               sealed class Outer : IDisposable
                               {
                                   public Outer(
                                       [Tag("first")] Owned<Resource> first,
                                       [Tag("second")] Owned<Resource> second)
                                   {
                                       First = first;
                                       Second = second;
                                   }

                                   public Owned<Resource> First { get; }

                                   public Owned<Resource> Second { get; }

                                   public int DisposeCount { get; private set; }

                                   public void Dispose() => DisposeCount++;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<Resource>("first", "second").To<Resource>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<Outer>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldIsolateDirectlyNestedOwnedBehindIntermediateDependency()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Owned<Outer> outer = composition.Root;
                                       Middle middle = outer.Value.Middle;
                                       Owned<Resource> inner = middle.Inner;

                                       inner.Dispose();

                                       Console.WriteLine(inner.Value.IsDisposed);
                                       Console.WriteLine(!middle.IsDisposed);
                                       Console.WriteLine(!outer.Value.IsDisposed);

                                       outer.Dispose();

                                       Console.WriteLine(middle.IsDisposed);
                                       Console.WriteLine(outer.Value.IsDisposed);
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Middle : IDisposable
                               {
                                   public Middle(Owned<Resource> inner) => Inner = inner;

                                   public Owned<Resource> Inner { get; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Outer : IDisposable
                               {
                                   public Outer(Middle middle) => Middle = middle;

                                   public Middle Middle { get; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Bind().To<Middle>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<Outer>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldKeepAsyncDirectlyNestedOwnedGraphIndependent()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static async Task Main()
                                   {
                                       var composition = new Composition();
                                       Owned<Outer> outer = composition.Root;
                                       Owned<AsyncResource> inner = outer.Value.Inner;

                                       await inner.DisposeAsync();

                                       Console.WriteLine(inner.Value.DisposeCount == 1);
                                       Console.WriteLine(outer.Value.DisposeCount == 0);

                                       await outer.DisposeAsync();

                                       Console.WriteLine(outer.Value.DisposeCount == 1);
                                       Console.WriteLine(inner.Value.DisposeCount == 1);
                                   }
                               }

                               sealed class AsyncResource : IAsyncDisposable
                               {
                                   public int DisposeCount { get; private set; }

                                   public ValueTask DisposeAsync()
                                   {
                                       DisposeCount++;
                                       return default;
                                   }
                               }

                               sealed class Outer : IAsyncDisposable
                               {
                                   public Outer(Owned<AsyncResource> inner) => Inner = inner;

                                   public Owned<AsyncResource> Inner { get; }

                                   public int DisposeCount { get; private set; }

                                   public ValueTask DisposeAsync()
                                   {
                                       DisposeCount++;
                                       return default;
                                   }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<AsyncResource>().To<AsyncResource>()
                                           .Bind<Outer>().To<Outer>()
                                           .Root<Owned<Outer>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldRollBackEverySiblingNestedOwnedInReverseConstructionOrder()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       try
                                       {
                                           _ = composition.Root;
                                       }
                                       catch (InvalidOperationException)
                                       {
                                       }

                                       Console.WriteLine(string.Join(",", Resource.DisposedIds));
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   private static int nextId;

                                   public Resource() => Id = ++nextId;

                                   public static List<int> DisposedIds { get; } = new List<int>();

                                   private int Id { get; }

                                   public void Dispose() => DisposedIds.Add(Id);
                               }

                               sealed class Outer
                               {
                                   public Outer(
                                       [Tag("first")] Owned<Resource> first,
                                       [Tag("second")] Owned<Resource> second) =>
                                       throw new InvalidOperationException("Cannot create outer.");
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<Resource>("first", "second").To<Resource>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<Outer>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2,1"], result);
    }

    [Fact]
    public async Task ShouldKeepNestedOwnedInnerAliveAfterOuterDisposal()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IOuter> outer = composition.Root;
                                       IInner inner = outer.Value.Inner;
                           
                                       Console.WriteLine(!(inner.IsDisposed));
                                       outer.Dispose();
                                       Console.WriteLine(!(inner.IsDisposed));
                                   }
                           
                               }
                           
                               interface IInner
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Inner : IInner, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               // A disposable that belongs to the OUTER graph only (never to the inner Owned).
                               interface ISibling
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Sibling : ISibling, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IOuter
                               {
                                   IInner Inner { get; }
                           
                                   ISibling Sibling { get; }
                           
                                   void DisposeInner();
                               }
                           
                               // Pure.DI outer: nests Pure.DI.Owned<IInner> obtained through Func<Owned<IInner>>.
                               sealed class Outer : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned;

                                   public Outer(Func<Owned<IInner>> innerFactory, ISibling sibling)
                                   {
                                       innerOwned = innerFactory();
                                       Sibling = sibling;
                                   }

                                   public IInner Inner => innerOwned.Value;

                                   public ISibling Sibling { get; }
                           
                                   public void DisposeInner() => innerOwned.Dispose();
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Inner>()
                                           .Bind().To<Sibling>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<IOuter>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldKeepOuterGraphAliveAfterNestedOwnedDisposal()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IOuter> outer = composition.Root;
                                       ISibling sibling = outer.Value.Sibling;
                           
                                       Console.WriteLine(!(sibling.IsDisposed));
                                       outer.Value.DisposeInner();
                                       Console.WriteLine(!(sibling.IsDisposed));
                                   }
                           
                               }
                           
                               interface IInner
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Inner : IInner, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               // A disposable that belongs to the OUTER graph only (never to the inner Owned).
                               interface ISibling
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Sibling : ISibling, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IOuter
                               {
                                   IInner Inner { get; }
                           
                                   ISibling Sibling { get; }
                           
                                   void DisposeInner();
                               }
                           
                               // Pure.DI outer: nests Pure.DI.Owned<IInner> obtained through Func<Owned<IInner>>.
                               sealed class Outer : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned;

                                   public Outer(Func<Owned<IInner>> innerFactory, ISibling sibling)
                                   {
                                       innerOwned = innerFactory();
                                       Sibling = sibling;
                                   }

                                   public IInner Inner => innerOwned.Value;

                                   public ISibling Sibling { get; }
                           
                                   public void DisposeInner() => innerOwned.Dispose();
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Inner>()
                                           .Bind().To<Sibling>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<IOuter>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeNestedOwnedInnerInstance()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IOuter> outer = composition.Root;
                                       IInner inner = outer.Value.Inner;
                           
                                       outer.Value.DisposeInner();
                                       Console.WriteLine(inner.IsDisposed);
                                   }
                           
                               }
                           
                               interface IInner
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Inner : IInner, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               // A disposable that belongs to the OUTER graph only (never to the inner Owned).
                               interface ISibling
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Sibling : ISibling, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IOuter
                               {
                                   IInner Inner { get; }
                           
                                   ISibling Sibling { get; }
                           
                                   void DisposeInner();
                               }
                           
                               // Pure.DI outer: nests Pure.DI.Owned<IInner> obtained through Func<Owned<IInner>>.
                               sealed class Outer : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned;

                                   public Outer(Func<Owned<IInner>> innerFactory, ISibling sibling)
                                   {
                                       innerOwned = innerFactory();
                                       Sibling = sibling;
                                   }

                                   public IInner Inner => innerOwned.Value;

                                   public ISibling Sibling { get; }
                           
                                   public void DisposeInner() => innerOwned.Dispose();
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Inner>()
                                           .Bind().To<Sibling>()
                                           .Bind().To<Outer>()
                                           .Root<Owned<IOuter>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldIsolateOwnedRoots()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IService> owned1 = composition.Root;
                                       Owned<IService> owned2 = composition.Root;
                                       IDependency dependency1 = owned1.Value.Dependency;
                                       IDependency dependency2 = owned2.Value.Dependency;
                           
                                       Console.WriteLine(!(ReferenceEquals(dependency1, dependency2)));
                           
                                       owned1.Dispose();
                                       Console.WriteLine(dependency1.IsDisposed);
                                       Console.WriteLine(!(dependency2.IsDisposed));
                           
                                       owned2.Dispose();
                                       Console.WriteLine(dependency2.IsDisposed);
                                   }
                           
                               }
                           
                               interface IDependency
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Dependency : IDependency, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IService
                               {
                                   IDependency Dependency { get; }
                               }
                           
                               sealed class Service : IService
                               {
                                   public Service(IDependency dependency) => Dependency = dependency;

                                   public IDependency Dependency { get; }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<Owned<IService>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeOwnedSingletonOnlyWithComposition()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IService> owned = composition.Root;
                                       IService service = owned.Value;
                           
                                       Console.WriteLine(!(service.IsDisposed));
                           
                                       // Disposing the Owned<T> must NOT dispose the shared singleton.
                                       owned.Dispose();
                                       Console.WriteLine(!(service.IsDisposed));
                           
                                       // The composition owns the singleton and must dispose it on exit.
                                       IDisposable? disposableComposition = composition as IDisposable;
                                       Console.WriteLine(disposableComposition is not null);
                                       if (disposableComposition is null) return;
                                       disposableComposition.Dispose();
                                       Console.WriteLine(service.IsDisposed);
                                   }
                           
                               }
                           
                               interface IService
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Service : IService, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Service>()
                                           .Root<Owned<IService>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldReuseSingletonAcrossOwnedRoots()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IService> owned1 = composition.Root;
                                       Owned<IService> owned2 = composition.Root;
                           
                                       Console.WriteLine(ReferenceEquals(owned1.Value, owned2.Value));
                                   }
                           
                               }
                           
                               interface IService
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Service : IService, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Service>()
                                           .Root<Owned<IService>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldCreateAndDisposeTwoTransientSharedDependencies()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       TransientSharedComposition composition = new TransientSharedComposition();
                                       Owned<IRoot> owned = composition.Root;
                                       IShared left = owned.Value.Left.Shared;
                                       IShared right = owned.Value.Right.Shared;
                           
                                       Console.WriteLine(!(ReferenceEquals(left, right)));
                           
                                       owned.Dispose();
                                       Console.WriteLine(Test.AreEqual(left.DisposeCount, 1));
                                       Console.WriteLine(Test.AreEqual(right.DisposeCount, 1));
                                   }
                           
                               }
                           
                               interface IShared
                               {
                                   int DisposeCount { get; }
                               }
                           
                               sealed class Shared : IShared, IDisposable
                               {
                                   public int DisposeCount { get; private set; }
                           
                                   public void Dispose() => DisposeCount++;
                               }
                           
                               interface ILeft
                               {
                                   IShared Shared { get; }
                               }
                           
                               sealed class Left : ILeft
                               {
                                   public Left(IShared shared) => Shared = shared;

                                   public IShared Shared { get; }
                               }
                           
                               interface IRight
                               {
                                   IShared Shared { get; }
                               }
                           
                               sealed class Right : IRight
                               {
                                   public Right(IShared shared) => Shared = shared;

                                   public IShared Shared { get; }
                               }
                           
                               interface IRoot
                               {
                                   ILeft Left { get; }
                           
                                   IRight Right { get; }
                               }
                           
                               sealed class Root : IRoot
                               {
                                   public Root(ILeft left, IRight right)
                                   {
                                       Left = left;
                                       Right = right;
                                   }

                                   public ILeft Left { get; }

                                   public IRight Right { get; }
                               }
                           
                               partial class TransientSharedComposition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(TransientSharedComposition))
                                           .Bind().To<Shared>()
                                           .Bind().To<Left>()
                                           .Bind().To<Right>()
                                           .Bind().To<Root>()
                                           .Root<Owned<IRoot>>("Root");
                               }
                           
                               partial class PerResolveSharedComposition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(PerResolveSharedComposition))
                                           .Bind().As(Lifetime.PerResolve).To<Shared>()
                                           .Bind().To<Left>()
                                           .Bind().To<Right>()
                                           .Bind().To<Root>()
                                           .Root<Owned<IRoot>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldCreateAndDisposeOneScopedSharedDependency()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       PerResolveSharedComposition composition = new PerResolveSharedComposition();
                                       Owned<IRoot> owned = composition.Root;
                                       IShared left = owned.Value.Left.Shared;
                                       IShared right = owned.Value.Right.Shared;
                           
                                       Console.WriteLine(ReferenceEquals(left, right));
                           
                                       owned.Dispose();
                                       Console.WriteLine(Test.AreEqual(left.DisposeCount, 1));
                                   }
                           
                               }
                           
                               interface IShared
                               {
                                   int DisposeCount { get; }
                               }
                           
                               sealed class Shared : IShared, IDisposable
                               {
                                   public int DisposeCount { get; private set; }
                           
                                   public void Dispose() => DisposeCount++;
                               }
                           
                               interface ILeft
                               {
                                   IShared Shared { get; }
                               }
                           
                               sealed class Left : ILeft
                               {
                                   public Left(IShared shared) => Shared = shared;

                                   public IShared Shared { get; }
                               }
                           
                               interface IRight
                               {
                                   IShared Shared { get; }
                               }
                           
                               sealed class Right : IRight
                               {
                                   public Right(IShared shared) => Shared = shared;

                                   public IShared Shared { get; }
                               }
                           
                               interface IRoot
                               {
                                   ILeft Left { get; }
                           
                                   IRight Right { get; }
                               }
                           
                               sealed class Root : IRoot
                               {
                                   public Root(ILeft left, IRight right)
                                   {
                                       Left = left;
                                       Right = right;
                                   }

                                   public ILeft Left { get; }

                                   public IRight Right { get; }
                               }
                           
                               partial class TransientSharedComposition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(TransientSharedComposition))
                                           .Bind().To<Shared>()
                                           .Bind().To<Left>()
                                           .Bind().To<Right>()
                                           .Bind().To<Root>()
                                           .Root<Owned<IRoot>>("Root");
                               }
                           
                               partial class PerResolveSharedComposition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(PerResolveSharedComposition))
                                           .Bind().As(Lifetime.PerResolve).To<Shared>()
                                           .Bind().To<Left>()
                                           .Bind().To<Right>()
                                           .Bind().To<Root>()
                                           .Root<Owned<IRoot>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeResourceReleasedBySingletonOwner()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       IPool pool = composition.Pool;
                           
                                       IResource resource = pool.AcquireAndRelease();
                           
                                       Console.WriteLine(resource.IsDisposed);
                                   }
                                   // container and is disposed when the container is disposed.
                                   private const bool ForgottenResourceDisposedByContainer = true;
                           
                               }
                           
                               interface IResource
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Resource : IResource, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IPool
                               {
                                   IResource AcquireAndRelease();
                           
                                   IResource AcquireAndForget();
                               }
                           
                               sealed class Pool : IPool
                               {
                                   private readonly Func<Owned<IResource>> resourceFactory;

                                   public Pool(Func<Owned<IResource>> resourceFactory) =>
                                       this.resourceFactory = resourceFactory;

                                   public IResource AcquireAndRelease()
                                   {
                                       using Owned<IResource> owned = resourceFactory();
                                       return owned.Value;
                                   }
                           
                                   public IResource AcquireAndForget()
                                   {
                                       // Deliberately drops the Owned<T> handle without disposing it.
                                       Owned<IResource> owned = resourceFactory();
                                       return owned.Value;
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Bind().As(Lifetime.Singleton).To<Pool>()
                                           .Root<IPool>("Pool");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotDisposeTransferredOwnedResourceWithComposition()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       IPool pool = composition.Pool;
                                       IResource resource = pool.AcquireAndForget();
                           
                                       Console.WriteLine(!(resource.IsDisposed));
                           
                                       (composition as IDisposable)?.Dispose();
                                       Console.WriteLine(Test.AreEqual(resource.IsDisposed, ResourceDisposedByComposition));
                                   }

                                   // This behavior is intentionally not changed. Creating Owned<T> transfers
                                   // ownership and disposal responsibility to the caller. Tracking the returned
                                   // graph in the composition as well would introduce two competing owners and
                                   // retain already transferred resources until the composition is disposed.
                                   // The caller must therefore dispose every Owned<T> handle it receives.
                                   private const bool ResourceDisposedByComposition = false;
                           
                               }
                           
                               interface IResource
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Resource : IResource, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IPool
                               {
                                   IResource AcquireAndRelease();
                           
                                   IResource AcquireAndForget();
                               }
                           
                               sealed class Pool : IPool
                               {
                                   private readonly Func<Owned<IResource>> resourceFactory;

                                   public Pool(Func<Owned<IResource>> resourceFactory) =>
                                       this.resourceFactory = resourceFactory;

                                   public IResource AcquireAndRelease()
                                   {
                                       using Owned<IResource> owned = resourceFactory();
                                       return owned.Value;
                                   }
                           
                                   public IResource AcquireAndForget()
                                   {
                                       // Deliberately drops the Owned<T> handle without disposing it.
                                       Owned<IResource> owned = resourceFactory();
                                       return owned.Value;
                                   }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Bind().As(Lifetime.Singleton).To<Pool>()
                                           .Root<IPool>("Pool");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeSingletonThroughOwnedOnlyWithComposition()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IConsumer> owned = composition.Root;
                                       IConnection connection = owned.Value.Connection;
                           
                                       Console.WriteLine(!(connection.IsDisposed));
                           
                                       // Disposing the Owned<T> releases ownership but not the singleton.
                                       owned.Dispose();
                                       Console.WriteLine(!(connection.IsDisposed));
                           
                                       // Disposing the composition disposes the singleton.
                                       composition.Dispose();
                                       Console.WriteLine(connection.IsDisposed);
                                   }
                           
                               }
                           
                               interface IConnection
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Connection : IConnection, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IConsumer
                               {
                                   IConnection Connection { get; }
                               }
                           
                               sealed class Consumer : IConsumer
                               {
                                   public Consumer(IConnection connection) => Connection = connection;

                                   public IConnection Connection { get; }
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Singleton).To<Connection>()
                                           .Bind().To<Consumer>()
                                           .Root<Owned<IConsumer>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeTransientDependencyWithOwned()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                                   public static void Main()
                                   {
                                       // The composition itself owns nothing disposable here (everything is
                                       // owned by the Owned<T> root), so it does not implement IDisposable.
                                       Composition composition = new Composition();
                                       Owned<IService> owned = composition.Root;
                                       IDependency dependency = owned.Value.Dependency;
                           
                                       Console.WriteLine(!(dependency.IsDisposed));
                                       owned.Dispose();
                                       Console.WriteLine(dependency.IsDisposed);
                                   }
                           
                               }
                           
                               interface IDependency
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Dependency : IDependency, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IService
                               {
                                   IDependency Dependency { get; }
                           
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Service : IService, IDisposable
                               {
                                   public Service(IDependency dependency) => Dependency = dependency;

                                   public IDependency Dependency { get; }
                           
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<Owned<IService>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeTransientServiceWithOwned()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           
                           namespace Sample
                           {
                               public class Program
                               {
                           
                                   public static void Main()
                                   {
                                       Composition composition = new Composition();
                                       Owned<IService> owned = composition.Root;
                                       IService service = owned.Value;
                           
                                       Console.WriteLine(!(service.IsDisposed));
                                       owned.Dispose();
                                       Console.WriteLine(service.IsDisposed);
                                   }
                           
                               }
                           
                               interface IDependency
                               {
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Dependency : IDependency, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               interface IService
                               {
                                   IDependency Dependency { get; }
                           
                                   bool IsDisposed { get; }
                               }
                           
                               sealed class Service : IService, IDisposable
                               {
                                   public Service(IDependency dependency) => Dependency = dependency;

                                   public IDependency Dependency { get; }
                           
                                   public bool IsDisposed { get; private set; }
                           
                                   public void Dispose() => IsDisposed = true;
                               }
                           
                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Dependency>()
                                           .Bind().To<Service>()
                                           .Root<Owned<IService>>("Root");
                               }
                           
                               internal static class Test
                               {
                                   public static bool AreEqual<T>(T actual, T expected) =>
                                       EqualityComparer<T>.Default.Equals(actual, expected);
                           
                                   public static bool AreEqual<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) =>
                                       actual.SequenceEqual(expected);
                           
                                   public static Exception? RecordException(Action action)
                                   {
                                       try
                                       {
                                           action();
                                           return null;
                                       }
                                       catch (Exception exception)
                                       {
                                           return exception;
                                       }
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldIsolateUserDefinedAccumulatorsForDifferentOwnedTypes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factories = composition.Factories;
                                       var first = factories.first();
                                       var second = factories.second();

                                       first.Dispose();
                                       Console.WriteLine(first.Value.IsDisposed);
                                       Console.WriteLine(!second.Value.IsDisposed);

                                       second.Dispose();
                                       Console.WriteLine(second.Value.IsDisposed);
                                   }
                               }

                               interface ICustomOwned : IDisposable
                               {
                               }

                               sealed class CustomAccumulator : List<object>, ICustomOwned
                               {
                                   public void Dispose()
                                   {
                                       for (var i = Count - 1; i >= 0; i--)
                                       {
                                           if (this[i] is IDisposable disposable and not ICustomOwned)
                                           {
                                               disposable.Dispose();
                                           }
                                       }
                                   }
                               }

                               readonly struct CustomOwned<T> : ICustomOwned
                               {
                                   private readonly ICustomOwned owned;

                                   public CustomOwned(T value, ICustomOwned owned)
                                   {
                                       Value = value;
                                       this.owned = owned;
                                   }

                                   public T Value { get; }

                                   public void Dispose() => owned.Dispose();
                               }

                               interface IFirst
                               {
                                   bool IsDisposed { get; }
                               }

                               sealed class First : IFirst, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               interface ISecond
                               {
                                   bool IsDisposed { get; }
                               }

                               sealed class Second : ISecond, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Accumulate<IDisposable, CustomAccumulator>(Transient, PerResolve, PerBlock)
                                           .Bind<ICustomOwned>().To((CustomAccumulator accumulator) => accumulator)
                                           .Bind<CustomOwned<TT>>().As(PerBlock).To(ctx =>
                                           {
                                               ctx.Inject<ICustomOwned>(out var owned);
                                               ctx.Inject<TT>(ctx.Tag, out var value);
                                               return new CustomOwned<TT>(value, owned);
                                           })
                                           .Bind().To<First>()
                                           .Bind().To<Second>()
                                           .Root<(
                                               Func<CustomOwned<IFirst>> first,
                                               Func<CustomOwned<ISecond>> second)>("Factories");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeCopiedOwnedHandleOnlyOnce()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var first = composition.Root;
                                       var second = first;
                                       var resource = first.Value;

                                       first.Dispose();
                                       second.Dispose();

                                       Console.WriteLine(resource.DisposeCount == 1);
                                   }
                               }

                               interface IResource
                               {
                                   int DisposeCount { get; }
                               }

                               sealed class Resource : IResource, IDisposable
                               {
                                   public int DisposeCount { get; private set; }

                                   public void Dispose() => DisposeCount++;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Owned<IResource>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeCreatedResourcesWhenOwnedGraphConstructionFails()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();

                                       try
                                       {
                                           _ = composition.Root;
                                       }
                                       catch (InvalidOperationException)
                                       {
                                       }

                                       Console.WriteLine(Resource.LastCreated?.IsDisposed == true);
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   public Resource() => LastCreated = this;

                                   public static Resource? LastCreated { get; private set; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class FailingDependency
                               {
                                   public FailingDependency(Resource resource)
                                   {
                                       _ = resource;
                                       throw new InvalidOperationException("Construction failed.");
                                   }
                               }

                               sealed class Root
                               {
                                   public Root(FailingDependency dependency) => Dependency = dependency;

                                   public FailingDependency Dependency { get; }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Bind().To<FailingDependency>()
                                           .Bind().To<Root>()
                                           .Root<Owned<Root>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldAllowTransferredOwnedToBeDisposedAfterComposition()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var resource = owned.Value;

                                       (composition as IDisposable)?.Dispose();
                                       Console.WriteLine(!resource.IsDisposed);

                                       owned.Dispose();
                                       Console.WriteLine(resource.IsDisposed);
                                   }
                               }

                               interface IResource
                               {
                                   bool IsDisposed { get; }
                               }

                               sealed class Resource : IResource, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Owned<IResource>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldCreateIndependentOwnedGraphsConcurrently()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Concurrent;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static async Task Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       var handles = new ConcurrentBag<Owned<IResource>>();

                                       var createTasks = Enumerable.Range(0, 32)
                                           .Select(_ => Task.Run(() => handles.Add(factory())))
                                           .ToArray();
                                       await Task.WhenAll(createTasks);

                                       var resources = handles.Select(i => i.Value).ToArray();
                                       var disposeTasks = handles
                                           .Select(owned => Task.Run(owned.Dispose))
                                           .ToArray();
                                       await Task.WhenAll(disposeTasks);

                                       Console.WriteLine(resources.Distinct().Count() == resources.Length);
                                       Console.WriteLine(resources.All(i => i.DisposeCount == 1));
                                   }
                               }

                               interface IResource
                               {
                                   int DisposeCount { get; }
                               }

                               sealed class Resource : IResource, IDisposable
                               {
                                   public int DisposeCount { get; private set; }

                                   public void Dispose() => DisposeCount++;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Func<Owned<IResource>>>("Factory");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeOwnedOnlyOnceWhenDisposeCallsRace()
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
                               public static class Program
                               {
                                   public static async Task Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       var failures = 0;

                                       var iterations = Enumerable.Range(0, 200)
                                           .Select(async _ =>
                                           {
                                               var owned = factory();
                                               var resource = owned.Value;
                                               using var start = new ManualResetEventSlim(false);
                                               var first = Task.Run(() =>
                                               {
                                                   start.Wait();
                                                   owned.Dispose();
                                               });
                                               var second = Task.Run(() =>
                                               {
                                                   start.Wait();
                                                   owned.Dispose();
                                               });

                                               start.Set();
                                               await Task.WhenAll(first, second);
                                               if (resource.DisposeCount != 1)
                                               {
                                                   Interlocked.Increment(ref failures);
                                               }
                                           })
                                           .ToArray();
                                       await Task.WhenAll(iterations);

                                       Console.WriteLine(failures == 0);
                                   }
                               }

                               interface IResource
                               {
                                   int DisposeCount { get; }
                               }

                               sealed class Resource : IResource, IDisposable
                               {
                                   private int disposeCount;

                                   public int DisposeCount => disposeCount;

                                   public void Dispose() => Interlocked.Increment(ref disposeCount);
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Func<Owned<IResource>>>("Factory");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSuppressAsyncDisposalExceptionAndContinueDisposing()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main() => Run().GetAwaiter().GetResult();

                                   private static async Task Run()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var root = owned.Value;

                                       Exception? exception = null;
                                       try
                                       {
                                           await owned.DisposeAsync();
                                       }
                                       catch (Exception caught)
                                       {
                                           exception = caught;
                                       }

                                       Console.WriteLine(exception is null);
                                       Console.WriteLine(root.Before.IsDisposed);
                                       Console.WriteLine(root.Thrower.DisposeAttempted);
                                       Console.WriteLine(root.After.IsDisposed);
                                   }
                               }

                               sealed class AsyncResource : IAsyncDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public ValueTask DisposeAsync()
                                   {
                                       IsDisposed = true;
                                       return default;
                                   }
                               }

                               sealed class AsyncThrower : IAsyncDisposable
                               {
                                   public bool DisposeAttempted { get; private set; }

                                   public async ValueTask DisposeAsync()
                                   {
                                       DisposeAttempted = true;
                                       await Task.Yield();
                                       throw new InvalidOperationException("Disposal failed.");
                                   }
                               }

                               sealed class Root
                               {
                                   public Root(
                                       [Tag("before")] AsyncResource before,
                                       AsyncThrower thrower,
                                       [Tag("after")] AsyncResource after)
                                   {
                                       Before = before;
                                       Thrower = thrower;
                                       After = after;
                                   }

                                   public AsyncResource Before { get; }

                                   public AsyncThrower Thrower { get; }

                                   public AsyncResource After { get; }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind("before", "after").To<AsyncResource>()
                                           .Bind().To<AsyncThrower>()
                                           .Bind().To<Root>()
                                           .Root<Owned<Root>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldReportCompletedAsyncDisposalFailureDuringSynchronousDisposal()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Pure.DI
                           {
                               internal sealed partial class Owned
                               {
                                   public static int AsyncDisposalFailureCount { get; private set; }

                                   partial void OnDisposeAsyncException<T>(T instance, Exception exception)
                                       where T : IAsyncDisposable =>
                                       AsyncDisposalFailureCount++;
                               }
                           }

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;

                                       owned.Dispose();

                                       Console.WriteLine(owned.Value.DisposeAttempted);
                                       Console.WriteLine(Pure.DI.Owned.AsyncDisposalFailureCount == 1);
                                   }
                               }

                               sealed class AsyncThrower : IAsyncDisposable
                               {
                                   public bool DisposeAttempted { get; private set; }

                                   public ValueTask DisposeAsync()
                                   {
                                       DisposeAttempted = true;
                                       return new ValueTask(Task.FromException(
                                           new InvalidOperationException("Disposal failed.")));
                                   }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<AsyncThrower>()
                                           .Root<Owned<AsyncThrower>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeScopedResourceOnlyWithComposition()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var first = composition.Root;
                                       var second = composition.Root;
                                       var resource = first.Value;

                                       Console.WriteLine(ReferenceEquals(resource, second.Value));

                                       first.Dispose();
                                       second.Dispose();
                                       Console.WriteLine(!resource.IsDisposed);

                                       composition.Dispose();
                                       Console.WriteLine(resource.IsDisposed);
                                   }
                               }

                               interface IResource
                               {
                                   bool IsDisposed { get; }
                               }

                               sealed class Resource : IResource, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().As(Lifetime.Scoped).To<Resource>()
                                           .Root<Owned<IResource>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldIsolateOwnedFactoriesWithArguments()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       var first = factory("first");
                                       var second = factory("second");

                                       Console.WriteLine(first.Value.Name == "first");
                                       Console.WriteLine(second.Value.Name == "second");

                                       first.Dispose();
                                       Console.WriteLine(first.Value.IsDisposed);
                                       Console.WriteLine(!second.Value.IsDisposed);

                                       second.Dispose();
                                       Console.WriteLine(second.Value.IsDisposed);
                                   }
                               }

                               interface IResource
                               {
                                   string Name { get; }

                                   bool IsDisposed { get; }
                               }

                               sealed class Resource : IResource, IDisposable
                               {
                                   public Resource(string name) => Name = name;

                                   public string Name { get; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Func<string, Owned<IResource>>>("Factory");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldIsolateTaggedOwnedFactoriesOfSameType()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var first = composition.FirstFactory();
                                       var second = composition.SecondFactory();

                                       Console.WriteLine(first.Value.Name == "first");
                                       Console.WriteLine(second.Value.Name == "second");

                                       first.Dispose();
                                       Console.WriteLine(first.Value.IsDisposed);
                                       Console.WriteLine(!second.Value.IsDisposed);

                                       second.Dispose();
                                       Console.WriteLine(second.Value.IsDisposed);
                                   }
                               }

                               interface IResource
                               {
                                   string Name { get; }

                                   bool IsDisposed { get; }
                               }

                               sealed class FirstResource : IResource, IDisposable
                               {
                                   public string Name => "first";

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class SecondResource : IResource, IDisposable
                               {
                                   public string Name => "second";

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IResource>("first").To<FirstResource>()
                                           .Bind<IResource>("second").To<SecondResource>()
                                           .Root<Func<Owned<IResource>>>("FirstFactory", "first")
                                           .Root<Func<Owned<IResource>>>("SecondFactory", "second");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeAsyncOwnedGraphInReverseConstructionOrder()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main() => Run().GetAwaiter().GetResult();

                                   private static async Task Run()
                                   {
                                       var recorder = new DisposalRecorder();
                                       var composition = new Composition(recorder);
                                       var owned = composition.Root;

                                       await owned.DisposeAsync();

                                       Console.WriteLine(string.Join(",", recorder.Events) == "root,middle,leaf");
                                   }
                               }

                               sealed class DisposalRecorder
                               {
                                   public List<string> Events { get; } = new();
                               }

                               sealed class Leaf : IAsyncDisposable
                               {
                                   private readonly DisposalRecorder recorder;

                                   public Leaf(DisposalRecorder recorder) => this.recorder = recorder;

                                   public ValueTask DisposeAsync()
                                   {
                                       recorder.Events.Add("leaf");
                                       return default;
                                   }
                               }

                               sealed class Middle : IAsyncDisposable
                               {
                                   private readonly DisposalRecorder recorder;

                                   public Middle(Leaf leaf, DisposalRecorder recorder)
                                   {
                                       Leaf = leaf;
                                       this.recorder = recorder;
                                   }

                                   public Leaf Leaf { get; }

                                   public ValueTask DisposeAsync()
                                   {
                                       recorder.Events.Add("middle");
                                       return default;
                                   }
                               }

                               sealed class Root : IAsyncDisposable
                               {
                                   private readonly DisposalRecorder recorder;

                                   public Root(Middle middle, DisposalRecorder recorder)
                                   {
                                       Middle = middle;
                                       this.recorder = recorder;
                                   }

                                   public Middle Middle { get; }

                                   public ValueTask DisposeAsync()
                                   {
                                       recorder.Events.Add("root");
                                       return default;
                                   }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Arg<DisposalRecorder>("recorder")
                                           .Bind<Leaf>().To<Leaf>()
                                           .Bind<Middle>().To<Middle>()
                                           .Bind<Root>().To<Root>()
                                           .Root<Owned<Root>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeOwnedOnlyOnceWhenDisposedAsynchronouslyThenSynchronously()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main() => Run().GetAwaiter().GetResult();

                                   private static async Task Run()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var resource = owned.Value;

                                       await owned.DisposeAsync();
                                       owned.Dispose();

                                       Console.WriteLine(resource.DisposeCount == 1);
                                   }
                               }

                               sealed class Resource : IDisposable, IAsyncDisposable
                               {
                                   public int DisposeCount { get; private set; }

                                   public void Dispose() => DisposeCount++;

                                   public ValueTask DisposeAsync()
                                   {
                                       DisposeCount++;
                                       return default;
                                   }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Owned<Resource>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldTrackConcurrentlyCreatedDependenciesInSameOwnedGraph()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Concurrent;
                           using System.Linq;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var widgets = new ConcurrentBag<IWidget>();

                                       var tasks = Enumerable.Range(0, 64)
                                           .Select(_ => Task.Run(() => widgets.Add(owned.Value.CreateWidget())))
                                           .ToArray();
                                       Task.WaitAll(tasks);

                                       owned.Dispose();

                                       Console.WriteLine(widgets.Count == 64);
                                       Console.WriteLine(widgets.Distinct().Count() == 64);
                                       Console.WriteLine(widgets.All(i => i.DisposeCount == 1));
                                   }
                               }

                               interface IWidget
                               {
                                   int DisposeCount { get; }
                               }

                               sealed class Widget : IWidget, IDisposable
                               {
                                   public int DisposeCount { get; private set; }

                                   public void Dispose() => DisposeCount++;
                               }

                               sealed class Root
                               {
                                   private readonly Func<IWidget> widgetFactory;

                                   public Root(Func<IWidget> widgetFactory) => this.widgetFactory = widgetFactory;

                                   public IWidget CreateWidget() => widgetFactory();
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Widget>()
                                           .Bind().To<Root>()
                                           .Root<Owned<Root>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeEveryResourceInOwnedCollection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var resources = owned.Value;

                                       Console.WriteLine(resources.Count == 3);
                                       Console.WriteLine(resources.All(i => !i.IsDisposed));

                                       owned.Dispose();
                                       Console.WriteLine(resources.All(i => i.IsDisposed));
                                   }
                               }

                               interface IResource
                               {
                                   bool IsDisposed { get; }
                               }

                               abstract class ResourceBase : IResource, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class FirstResource : ResourceBase
                               {
                               }

                               sealed class SecondResource : ResourceBase
                               {
                               }

                               sealed class ThirdResource : ResourceBase
                               {
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IResource>(1).To<FirstResource>()
                                           .Bind<IResource>(2).To<SecondResource>()
                                           .Bind<IResource>(3).To<ThirdResource>()
                                           .Root<Owned<IReadOnlyList<IResource>>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldDisposeOwnedElementsInCollectionIndependently()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var resources = composition.Root.Create();
                                       var first = resources[0];
                                       var second = resources[1];

                                       first.Dispose();
                                       Console.WriteLine(first.Value.IsDisposed);
                                       Console.WriteLine(!second.Value.IsDisposed);

                                       second.Dispose();
                                       Console.WriteLine(second.Value.IsDisposed);
                                   }
                               }

                               interface IResource
                               {
                                   bool IsDisposed { get; }
                               }

                               abstract class ResourceBase : IResource, IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class FirstResource : ResourceBase
                               {
                               }

                               sealed class SecondResource : ResourceBase
                               {
                               }

                               sealed class ResourceSet
                               {
                                   private readonly Func<Owned<IResource>> firstFactory;
                                   private readonly Func<Owned<IResource>> secondFactory;

                                   public ResourceSet(
                                       [Tag(1)] Func<Owned<IResource>> firstFactory,
                                       [Tag(2)] Func<Owned<IResource>> secondFactory)
                                   {
                                       this.firstFactory = firstFactory;
                                       this.secondFactory = secondFactory;
                                   }

                                   public IReadOnlyList<Owned<IResource>> Create() =>
                                       new[] { firstFactory(), secondFactory() };
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IResource>(1).To<FirstResource>()
                                           .Bind<IResource>(2).To<SecondResource>()
                                           .Bind().To<ResourceSet>()
                                           .Root<ResourceSet>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldNotGenerateSynchronizationForOwnedWhenThreadSafetyIsDisabled()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var resource = owned.Value;

                                       owned.Dispose();
                                       owned.Dispose();

                                       Console.WriteLine(resource.DisposeCount == 1);
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   public int DisposeCount { get; private set; }

                                   public void Dispose() => DisposeCount++;
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Bind().To<Resource>()
                                           .Root<Owned<Resource>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
        result.GeneratedCode.ShouldNotContain("lock (_lock");
        result.GeneratedCode.ShouldNotContain("global::System.Threading.Interlocked.Exchange");
        result.GeneratedCode.ShouldNotContain("var items = ToArray()");
        result.GeneratedCode.ShouldNotContain("global::System.Action _enter");
        result.GeneratedCode.ShouldNotContain("global::System.Action _exit");
    }

    [Fact]
    public async Task ShouldRollBackUserDefinedAccumulatorWhenGraphConstructionFails()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();

                                       try
                                       {
                                           _ = composition.Root;
                                       }
                                       catch (InvalidOperationException)
                                       {
                                       }

                                       Console.WriteLine(Resource.LastCreated?.IsDisposed == true);
                                   }
                               }

                               interface ICustomOwner : IDisposable
                               {
                               }

                               sealed class CustomAccumulator : List<object>, ICustomOwner
                               {
                                   public void Dispose()
                                   {
                                       for (var index = Count - 1; index >= 0; index--)
                                       {
                                           if (!ReferenceEquals(this, this[index])
                                               && this[index] is IDisposable disposable)
                                           {
                                               disposable.Dispose();
                                           }
                                       }

                                       Clear();
                                   }
                               }

                               readonly struct CustomOwned<T> : IDisposable
                               {
                                   private readonly ICustomOwner owner;

                                   public CustomOwned(T value, ICustomOwner owner)
                                   {
                                       Value = value;
                                       this.owner = owner;
                                   }

                                   public T Value { get; }

                                   public void Dispose() => owner.Dispose();
                               }

                               sealed class Resource : IDisposable
                               {
                                   public Resource() => LastCreated = this;

                                   public static Resource? LastCreated { get; private set; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class FailingDependency
                               {
                                   public FailingDependency(Resource resource)
                                   {
                                       _ = resource;
                                       throw new InvalidOperationException("Construction failed.");
                                   }
                               }

                               sealed class Root
                               {
                                   public Root(FailingDependency dependency) => Dependency = dependency;

                                   public FailingDependency Dependency { get; }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Accumulate<IDisposable, CustomAccumulator>(Transient, PerResolve, PerBlock)
                                           .Bind<ICustomOwner>().To((CustomAccumulator accumulator) => accumulator)
                                           .Bind<CustomOwned<TT>>().As(PerBlock).To(ctx =>
                                           {
                                               ctx.Inject<ICustomOwner>(out var owner);
                                               ctx.Inject<TT>(ctx.Tag, out var value);
                                               return new CustomOwned<TT>(value, owner);
                                           })
                                           .Bind().To<Resource>()
                                           .Bind().To<FailingDependency>()
                                           .Bind().To<Root>()
                                           .Root<CustomOwned<Root>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldRollBackAsyncOnlyUserDefinedAccumulatorWhenGraphConstructionFails()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Threading.Tasks;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();

                                       try
                                       {
                                           _ = composition.Root;
                                       }
                                       catch (InvalidOperationException)
                                       {
                                       }

                                       Console.WriteLine(AsyncResource.LastCreated?.IsDisposed == true);
                                   }
                               }

                               interface IAsyncOwner : IAsyncDisposable
                               {
                               }

                               sealed class AsyncAccumulator : List<object>, IAsyncOwner
                               {
                                   public async ValueTask DisposeAsync()
                                   {
                                       for (var index = Count - 1; index >= 0; index--)
                                       {
                                           if (!ReferenceEquals(this, this[index])
                                               && this[index] is IAsyncDisposable asyncDisposable)
                                           {
                                               await asyncDisposable.DisposeAsync();
                                           }
                                       }

                                       Clear();
                                   }
                               }

                               readonly struct AsyncOwned<T> : IAsyncDisposable
                               {
                                   private readonly IAsyncOwner owner;

                                   public AsyncOwned(T value, IAsyncOwner owner)
                                   {
                                       Value = value;
                                       this.owner = owner;
                                   }

                                   public T Value { get; }

                                   public ValueTask DisposeAsync() => owner.DisposeAsync();
                               }

                               sealed class AsyncResource : IAsyncDisposable
                               {
                                   public AsyncResource() => LastCreated = this;

                                   public static AsyncResource? LastCreated { get; private set; }

                                   public bool IsDisposed { get; private set; }

                                   public ValueTask DisposeAsync()
                                   {
                                       IsDisposed = true;
                                       return default;
                                   }
                               }

                               sealed class FailingDependency
                               {
                                   public FailingDependency(AsyncResource resource)
                                   {
                                       _ = resource;
                                       throw new InvalidOperationException("Construction failed.");
                                   }
                               }

                               sealed class Root
                               {
                                   public Root(FailingDependency dependency) => Dependency = dependency;

                                   public FailingDependency Dependency { get; }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Accumulate<IAsyncDisposable, AsyncAccumulator>(Transient, PerResolve, PerBlock)
                                           .Bind<IAsyncOwner>().To((AsyncAccumulator accumulator) => accumulator)
                                           .Bind<AsyncOwned<TT>>().As(PerBlock).To(ctx =>
                                           {
                                               ctx.Inject<IAsyncOwner>(out var owner);
                                               ctx.Inject<TT>(ctx.Tag, out var value);
                                               return new AsyncOwned<TT>(value, owner);
                                           })
                                           .Bind().To<AsyncResource>()
                                           .Bind().To<FailingDependency>()
                                           .Bind().To<Root>()
                                           .Root<AsyncOwned<Root>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldRejectResourceCreationAfterOwnedIsDisposed()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var root = owned.Value;

                                       owned.Dispose();

                                       var rejected = false;
                                       try
                                       {
                                           _ = root.CreateResource();
                                       }
                                       catch (ObjectDisposedException)
                                       {
                                           rejected = true;
                                       }

                                       Console.WriteLine(rejected);
                                       Console.WriteLine(Resource.LastCreated?.IsDisposed == true);
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   public Resource() => LastCreated = this;

                                   public static Resource? LastCreated { get; private set; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Root
                               {
                                   private readonly Func<Resource> resourceFactory;

                                   public Root(Func<Resource> resourceFactory) => this.resourceFactory = resourceFactory;

                                   public Resource CreateResource() => resourceFactory();
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Bind().To<Root>()
                                           .Root<Owned<Root>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldPreserveObjectDisposedExceptionWhenRejectedResourceDisposalThrows()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var root = owned.Value;
                                       owned.Dispose();

                                       Exception? exception = null;
                                       try
                                       {
                                           _ = root.CreateResource();
                                       }
                                       catch (Exception caught)
                                       {
                                           exception = caught;
                                       }

                                       Console.WriteLine(exception is ObjectDisposedException);
                                       Console.WriteLine(Resource.LastCreated?.DisposalAttempted == true);
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   public Resource() => LastCreated = this;

                                   public static Resource? LastCreated { get; private set; }

                                   public bool DisposalAttempted { get; private set; }

                                   public void Dispose()
                                   {
                                       DisposalAttempted = true;
                                       throw new InvalidOperationException("Disposal failed.");
                                   }
                               }

                               sealed class Root
                               {
                                   private readonly Func<Resource> resourceFactory;

                                   public Root(Func<Resource> resourceFactory) => this.resourceFactory = resourceFactory;

                                   public Resource CreateResource() => resourceFactory();
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Bind().To<Root>()
                                           .Root<Owned<Root>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldPreserveObjectDisposedExceptionWhenRejectedAsyncResourceDisposalThrows()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var root = owned.Value;
                                       owned.Dispose();

                                       Exception? exception = null;
                                       try
                                       {
                                           _ = root.CreateResource();
                                       }
                                       catch (Exception caught)
                                       {
                                           exception = caught;
                                       }

                                       Console.WriteLine(exception is ObjectDisposedException);
                                       Console.WriteLine(Resource.LastCreated?.DisposalAttempted == true);
                                   }
                               }

                               sealed class Resource : IAsyncDisposable
                               {
                                   public Resource() => LastCreated = this;

                                   public static Resource? LastCreated { get; private set; }

                                   public bool DisposalAttempted { get; private set; }

                                   public async ValueTask DisposeAsync()
                                   {
                                       DisposalAttempted = true;
                                       await Task.Yield();
                                       throw new InvalidOperationException("Async disposal failed.");
                                   }
                               }

                               sealed class Root
                               {
                                   private readonly Func<Resource> resourceFactory;

                                   public Root(Func<Resource> resourceFactory) => this.resourceFactory = resourceFactory;

                                   public Resource CreateResource() => resourceFactory();
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Bind().To<Root>()
                                           .Root<Owned<Root>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldSynchronizeResourceCreationWithOwnedDisposal()
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
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var root = owned.Value;

                                       var createTask = Task.Run(() =>
                                       {
                                           try
                                           {
                                               _ = root.CreateResource();
                                               return false;
                                           }
                                           catch (ObjectDisposedException)
                                           {
                                               return true;
                                           }
                                       });

                                       Resource.Created.Wait();
                                       owned.Dispose();
                                       Resource.Continue.Set();

                                       Console.WriteLine(createTask.GetAwaiter().GetResult());
                                       Console.WriteLine(Resource.LastCreated?.IsDisposed == true);
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   public static readonly ManualResetEventSlim Created = new(false);
                                   public static readonly ManualResetEventSlim Continue = new(false);

                                   public Resource()
                                   {
                                       LastCreated = this;
                                       Created.Set();
                                       Continue.Wait();
                                   }

                                   public static Resource? LastCreated { get; private set; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Root
                               {
                                   private readonly Func<Resource> resourceFactory;

                                   public Root(Func<Resource> resourceFactory) => this.resourceFactory = resourceFactory;

                                   public Resource CreateResource() => resourceFactory();
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Bind().To<Root>()
                                           .Root<Owned<Root>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Theory]
    [InlineData("", "True", false)]
#if ROSLYN5_6_OR_GREATER
    [InlineData("", "True", true)]
    [InlineData(".Hint(Hint.ThreadSafe, \"Off\")", "False", true)]
#else
    [InlineData(".Hint(Hint.ThreadSafe, \"Off\")", "False", false)]
#endif
    public async Task ShouldInitializeUserDefinedAccumulatorOnlyWhenThreadSafe(
        string threadSafeHint,
        string expectedInitialization,
        bool useSystemThreadingLock)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Threading;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;
                                       root.Dispose();

                                       Console.WriteLine(CustomAccumulator.WasInitialized);
                                       Console.WriteLine(CustomAccumulator.WasDisposeSynchronized);
                                   }
                               }

                               interface ICustomOwner : IDisposable
                               {
                               }

                               sealed class CustomAccumulator : List<object>, ICustomOwner, IAccumulator
                               {
                           #if NET9_0_OR_GREATER
                                   private Lock? synchronization;
                           #else
                                   private object? synchronization;
                           #endif

                                   public static bool WasInitialized { get; private set; }

                                   public static bool WasDisposeSynchronized { get; private set; }

                           #if NET9_0_OR_GREATER
                                   void IAccumulator.Initialize(Lock value)
                           #else
                                   void IAccumulator.Initialize(object value)
                           #endif
                                   {
                                       synchronization = value;
                                       WasInitialized = true;
                                   }

                                   public void Dispose()
                                   {
                                       var value = synchronization;
                                       if (value is null)
                                       {
                                           Clear();
                                           return;
                                       }

                                       lock (value)
                                       {
                                           WasDisposeSynchronized = true;
                                           Clear();
                                       }
                                   }
                               }

                               readonly struct CustomOwned<T> : IDisposable
                               {
                                   private readonly ICustomOwner owner;

                                   public CustomOwned(T value, ICustomOwner owner)
                                   {
                                       Value = value;
                                       this.owner = owner;
                                   }

                                   public T Value { get; }

                                   public void Dispose() => owner.Dispose();
                               }

                               sealed class Resource : IDisposable
                               {
                                   public void Dispose()
                                   {
                                   }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           #threadSafeHint#
                                           .Accumulate<IDisposable, CustomAccumulator>(Transient, PerResolve, PerBlock)
                                           .Bind<ICustomOwner>().To((CustomAccumulator accumulator) => accumulator)
                                           .Bind<CustomOwned<TT>>().As(PerBlock).To(ctx =>
                                           {
                                               ctx.Inject<ICustomOwner>(out var owner);
                                               ctx.Inject<TT>(ctx.Tag, out var value);
                                               return new CustomOwned<TT>(value, owner);
                                           })
                                           .Bind().To<Resource>()
                                           .Root<CustomOwned<Resource>>("Root");
                               }
                           }
                           """
            .Replace("#threadSafeHint#", threadSafeHint)
            .RunAsync(new Options(
                LanguageVersion.Preview,
                PreprocessorSymbols: useSystemThreadingLock
                    ? ["NET", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER"]
                    : ["NET20"]));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe([expectedInitialization, expectedInitialization], result);
        if (string.IsNullOrEmpty(threadSafeHint))
        {
            global::System.Text.RegularExpressions.Regex.Matches(
                    result.GeneratedCode,
                    @"\(\(global::Pure\.DI\.IAccumulator\)\w+\)\.Initialize\(_lock\w*\);")
                .Count.ShouldBe(1, result);
            result.GeneratedCode.ShouldNotContain(" is global::Pure.DI.IAccumulator");
        }
        else
        {
            result.GeneratedCode.ShouldNotContain("((global::Pure.DI.IAccumulator)");
        }
    }

    [Theory]
    [InlineData("", "True", false)]
#if ROSLYN5_6_OR_GREATER
    [InlineData("", "True", true)]
    [InlineData(".Hint(Hint.ThreadSafe, \"Off\")", "False", true)]
#else
    [InlineData(".Hint(Hint.ThreadSafe, \"Off\")", "False", false)]
#endif
    public async Task ShouldShareCompositionSynchronizationBetweenUserDefinedAccumulators(
        string threadSafeHint,
        string expectedSynchronization,
        bool useSystemThreadingLock)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Threading;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var root = composition.Root;

                                       Console.WriteLine(root.firstAccumulator.IsInitialized);
                                       Console.WriteLine(root.secondAccumulator.IsInitialized);
                           #if NET9_0_OR_GREATER
                                       Console.WriteLine(
                                           root.firstAccumulator.Synchronization is not null
                                           && root.firstAccumulator.Synchronization
                                               == root.secondAccumulator.Synchronization);
                           #else
                                       Console.WriteLine(
                                           root.firstAccumulator.Synchronization is not null
                                           && ReferenceEquals(
                                               root.firstAccumulator.Synchronization,
                                               root.secondAccumulator.Synchronization));
                           #endif
                                   }
                               }

                               interface IFirstResource
                               {
                               }

                               sealed class FirstResource : IFirstResource
                               {
                               }

                               interface ISecondResource
                               {
                               }

                               sealed class SecondResource : ISecondResource
                               {
                               }

                               sealed class FirstAccumulator : List<IFirstResource>, IAccumulator
                               {
                           #if NET9_0_OR_GREATER
                                   private Lock? synchronization;
                           #else
                                   private object? synchronization;
                           #endif

                                   public bool IsInitialized => synchronization is not null;

                           #if NET9_0_OR_GREATER
                                   public Lock? Synchronization => synchronization;
                           #else
                                   public object? Synchronization => synchronization;
                           #endif

                           #if NET9_0_OR_GREATER
                                   void IAccumulator.Initialize(Lock value) => synchronization = value;
                           #else
                                   void IAccumulator.Initialize(object value) => synchronization = value;
                           #endif
                               }

                               sealed class SecondAccumulator : List<ISecondResource>, IAccumulator
                               {
                           #if NET9_0_OR_GREATER
                                   private Lock? synchronization;
                           #else
                                   private object? synchronization;
                           #endif

                                   public bool IsInitialized => synchronization is not null;

                           #if NET9_0_OR_GREATER
                                   public Lock? Synchronization => synchronization;
                           #else
                                   public object? Synchronization => synchronization;
                           #endif

                           #if NET9_0_OR_GREATER
                                   void IAccumulator.Initialize(Lock value) => synchronization = value;
                           #else
                                   void IAccumulator.Initialize(object value) => synchronization = value;
                           #endif
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           #threadSafeHint#
                                           .Accumulate<IFirstResource, FirstAccumulator>(Transient)
                                           .Accumulate<ISecondResource, SecondAccumulator>(Transient)
                                           .Bind().To<FirstResource>()
                                           .Bind().To<SecondResource>()
                                           .Root<(
                                               IFirstResource first,
                                               ISecondResource second,
                                               FirstAccumulator firstAccumulator,
                                               SecondAccumulator secondAccumulator)>("Root");
                               }
                           }
                           """
            .Replace("#threadSafeHint#", threadSafeHint)
            .RunAsync(new Options(
                LanguageVersion.Preview,
                PreprocessorSymbols: useSystemThreadingLock
                    ? ["NET", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER"]
                    : ["NET20"]));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(
            [expectedSynchronization, expectedSynchronization, expectedSynchronization],
            result);
        if (string.IsNullOrEmpty(threadSafeHint))
        {
            const string synchronizationPattern =
                @"\(\(global::Pure\.DI\.IAccumulator\)\w+\)\.Initialize\((?<lock>_lock\w*)\);";
            var synchronizationMatches = global::System.Text.RegularExpressions.Regex.Matches(
                result.GeneratedCode,
                synchronizationPattern);
            synchronizationMatches.Count.ShouldBe(2, result);
            synchronizationMatches[0].Groups["lock"].Value.ShouldBe(
                synchronizationMatches[1].Groups["lock"].Value,
                result);
            result.GeneratedCode.ShouldNotContain(" is global::Pure.DI.IAccumulator");
        }
        else
        {
            result.GeneratedCode.ShouldNotContain("((global::Pure.DI.IAccumulator)");
        }
    }

    [Fact]
    public async Task ShouldReturnFromConcurrentDisposeWhileFirstDisposalIsInProgress()
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
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var resource = owned.Value;

                                       var first = Task.Run(owned.Dispose);
                                       resource.DisposalStarted.Wait();
                                       var second = Task.Run(owned.Dispose);

                                       Console.WriteLine(second.Wait(TimeSpan.FromSeconds(5)));
                                       Console.WriteLine(!first.IsCompleted);

                                       resource.ContinueDisposal.Set();
                                       first.GetAwaiter().GetResult();
                                       Console.WriteLine(resource.DisposeCount == 1);
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   private int disposeCount;

                                   public ManualResetEventSlim DisposalStarted { get; } = new(false);

                                   public ManualResetEventSlim ContinueDisposal { get; } = new(false);

                                   public int DisposeCount => disposeCount;

                                   public void Dispose()
                                   {
                                       Interlocked.Increment(ref disposeCount);
                                       DisposalStarted.Set();
                                       ContinueDisposal.Wait();
                                   }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Owned<Resource>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldReturnFromConcurrentDisposeAndDisposeAsyncWhileAsyncDisposalIsInProgress()
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
                               public static class Program
                               {
                                   public static void Main() => Run().GetAwaiter().GetResult();

                                   private static async Task Run()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var resource = owned.Value;

                                       var first = owned.DisposeAsync().AsTask();
                                       await resource.DisposalStarted.Task;
                                       var secondAsync = owned.DisposeAsync().AsTask();
                                       var secondSync = Task.Run(owned.Dispose);

                                       Console.WriteLine(secondAsync.IsCompletedSuccessfully);
                                       Console.WriteLine(await Task.WhenAny(secondSync, Task.Delay(5000)) == secondSync);
                                       Console.WriteLine(!first.IsCompleted);

                                       resource.ContinueDisposal.TrySetResult(true);
                                       await first;
                                       Console.WriteLine(resource.DisposeCount == 1);
                                   }
                               }

                               sealed class Resource : IAsyncDisposable
                               {
                                   private int disposeCount;

                                   public TaskCompletionSource<bool> DisposalStarted { get; } =
                                       new(TaskCreationOptions.RunContinuationsAsynchronously);

                                   public TaskCompletionSource<bool> ContinueDisposal { get; } =
                                       new(TaskCreationOptions.RunContinuationsAsynchronously);

                                   public int DisposeCount => disposeCount;

                                   public async ValueTask DisposeAsync()
                                   {
                                       Interlocked.Increment(ref disposeCount);
                                       DisposalStarted.TrySetResult(true);
                                       await ContinueDisposal.Task;
                                   }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Owned<Resource>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldReturnFromReentrantDisposeWithoutDeadlock()
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
                               public static class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var resource = owned.Value;
                                       resource.Reenter = owned.Dispose;

                                       var disposeTask = Task.Run(owned.Dispose);

                                       Console.WriteLine(disposeTask.Wait(TimeSpan.FromSeconds(5)));
                                       Console.WriteLine(resource.DisposeCount == 1);
                                   }
                               }

                               sealed class Resource : IDisposable
                               {
                                   private int disposeCount;

                                   public Action? Reenter { get; set; }

                                   public int DisposeCount => disposeCount;

                                   public void Dispose()
                                   {
                                       Interlocked.Increment(ref disposeCount);
                                       Reenter!();
                                   }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Owned<Resource>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldReturnFromReentrantDisposeAsyncWithoutDeadlock()
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
                               public static class Program
                               {
                                   public static void Main() => Run().GetAwaiter().GetResult();

                                   private static async Task Run()
                                   {
                                       var composition = new Composition();
                                       var owned = composition.Root;
                                       var resource = owned.Value;
                                       resource.Reenter = owned.DisposeAsync;

                                       var disposeTask = owned.DisposeAsync().AsTask();

                                       Console.WriteLine(
                                           await Task.WhenAny(disposeTask, Task.Delay(5000)) == disposeTask);
                                       Console.WriteLine(resource.DisposeCount == 1);
                                   }
                               }

                               sealed class Resource : IAsyncDisposable
                               {
                                   private int disposeCount;

                                   public Func<ValueTask>? Reenter { get; set; }

                                   public int DisposeCount => disposeCount;

                                   public async ValueTask DisposeAsync()
                                   {
                                       Interlocked.Increment(ref disposeCount);
                                       await Reenter!();
                                   }
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind().To<Resource>()
                                           .Root<Owned<Resource>>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

}
