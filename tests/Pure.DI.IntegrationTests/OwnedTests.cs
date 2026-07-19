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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldSurfaceOwnedDisposalExceptionAndContinueDisposing()
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
                           
                                       Exception? thrown = Test.RecordException(() => owned.Dispose());
                           
                                       Console.WriteLine(Test.AreEqual(DescribeState(thrown, root), ExpectedState));
                                   }
                           
                                   private const string ExpectedState = "thrown=True; attempted=True";
                           
                                   private static string DescribeState(Exception? thrown, IRoot root) =>
                                       $"thrown={thrown is not null}; attempted={root.Thrower.DisposeAttempted}";
                           
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
                           
                               sealed class Root(IBefore before, IThrower thrower, IAfter after) : IRoot
                               {
                                   public IBefore Before { get; } = before;
                           
                                   public IThrower Thrower { get; } = thrower;
                           
                                   public IAfter After { get; } = after;
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                                       Console.WriteLine(Test.AreEqual(recorder.Names, ["A", "B", "C"]));
                                   }
                           
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Handler(IConnection connection) : IHandler
                               {
                                   public IConnection Connection { get; } = connection;
                           
                                   public void Handle()
                                   {
                                   }
                               }
                           
                               interface IMessagePump
                               {
                                   IReadOnlyList<IHandler> Handled { get; }
                           
                                   void Process(int count);
                               }
                           
                               sealed class MessagePump(Func<Owned<IHandler>> handlerFactory) : IMessagePump
                               {
                                   private readonly List<IHandler> handled = [];
                           
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Inner(IShared shared, IInnerLocal local) : IInner
                               {
                                   public IShared Shared { get; } = shared;
                           
                                   public IInnerLocal Local { get; } = local;
                               }
                           
                               interface IOuter
                               {
                                   IShared Shared { get; }
                           
                                   IOuterLocal Local { get; }
                           
                                   IInner Inner { get; }
                           
                                   void DisposeInner();
                               }
                           
                               sealed class Outer(Func<Owned<IInner>> innerFactory, IShared shared, IOuterLocal local) : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned = innerFactory();
                           
                                   public IShared Shared { get; } = shared;
                           
                                   public IOuterLocal Local { get; } = local;
                           
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Inner(IShared shared, IInnerLocal local) : IInner
                               {
                                   public IShared Shared { get; } = shared;
                           
                                   public IInnerLocal Local { get; } = local;
                               }
                           
                               interface IOuter
                               {
                                   IShared Shared { get; }
                           
                                   IOuterLocal Local { get; }
                           
                                   IInner Inner { get; }
                           
                                   void DisposeInner();
                               }
                           
                               sealed class Outer(Func<Owned<IInner>> innerFactory, IShared shared, IOuterLocal local) : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned = innerFactory();
                           
                                   public IShared Shared { get; } = shared;
                           
                                   public IOuterLocal Local { get; } = local;
                           
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Inner(IShared shared, IInnerLocal local) : IInner
                               {
                                   public IShared Shared { get; } = shared;
                           
                                   public IInnerLocal Local { get; } = local;
                               }
                           
                               interface IOuter
                               {
                                   IShared Shared { get; }
                           
                                   IOuterLocal Local { get; }
                           
                                   IInner Inner { get; }
                           
                                   void DisposeInner();
                               }
                           
                               sealed class Outer(Func<Owned<IInner>> innerFactory, IShared shared, IOuterLocal local) : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned = innerFactory();
                           
                                   public IShared Shared { get; } = shared;
                           
                                   public IOuterLocal Local { get; } = local;
                           
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
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
                               sealed class Outer(Func<Owned<IInner>> innerFactory, ISibling sibling) : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned = innerFactory();
                           
                                   public IInner Inner => innerOwned.Value;
                           
                                   public ISibling Sibling { get; } = sibling;
                           
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                               sealed class Outer(Func<Owned<IInner>> innerFactory, ISibling sibling) : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned = innerFactory();
                           
                                   public IInner Inner => innerOwned.Value;
                           
                                   public ISibling Sibling { get; } = sibling;
                           
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                               sealed class Outer(Func<Owned<IInner>> innerFactory, ISibling sibling) : IOuter
                               {
                                   private readonly Owned<IInner> innerOwned = innerFactory();
                           
                                   public IInner Inner => innerOwned.Value;
                           
                                   public ISibling Sibling { get; } = sibling;
                           
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Service(IDependency dependency) : IService
                               {
                                   public IDependency Dependency { get; } = dependency;
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Left(IShared shared) : ILeft
                               {
                                   public IShared Shared { get; } = shared;
                               }
                           
                               interface IRight
                               {
                                   IShared Shared { get; }
                               }
                           
                               sealed class Right(IShared shared) : IRight
                               {
                                   public IShared Shared { get; } = shared;
                               }
                           
                               interface IRoot
                               {
                                   ILeft Left { get; }
                           
                                   IRight Right { get; }
                               }
                           
                               sealed class Root(ILeft left, IRight right) : IRoot
                               {
                                   public ILeft Left { get; } = left;
                           
                                   public IRight Right { get; } = right;
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Left(IShared shared) : ILeft
                               {
                                   public IShared Shared { get; } = shared;
                               }
                           
                               interface IRight
                               {
                                   IShared Shared { get; }
                               }
                           
                               sealed class Right(IShared shared) : IRight
                               {
                                   public IShared Shared { get; } = shared;
                               }
                           
                               interface IRoot
                               {
                                   ILeft Left { get; }
                           
                                   IRight Right { get; }
                               }
                           
                               sealed class Root(ILeft left, IRight right) : IRoot
                               {
                                   public ILeft Left { get; } = left;
                           
                                   public IRight Right { get; } = right;
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Pool(Func<Owned<IResource>> resourceFactory) : IPool
                               {
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldDisposeForgottenResourceWithComposition()
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
                                       Console.WriteLine(Test.AreEqual(resource.IsDisposed, ForgottenResourceDisposedByContainer));
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
                           
                               sealed class Pool(Func<Owned<IResource>> resourceFactory) : IPool
                               {
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Consumer(IConnection connection) : IConsumer
                               {
                                   public IConnection Connection { get; } = connection;
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Service(IDependency dependency) : IService, IDisposable
                               {
                                   public IDependency Dependency { get; } = dependency;
                           
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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
                           
                               sealed class Service(IDependency dependency) : IService, IDisposable
                               {
                                   public IDependency Dependency { get; } = dependency;
                           
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
                           """.RunAsync(new Options(LanguageVersion.Preview));

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

                               interface ICustomOwned : IDisposable;

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

                               readonly struct CustomOwned<T>(T value, ICustomOwned owned) : ICustomOwned
                               {
                                   public T Value { get; } = value;

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
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

}
