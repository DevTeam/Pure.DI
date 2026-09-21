namespace Pure.DI.IntegrationTests;

/// <summary>
/// Tests for the OnDispose / OnDisposeAsync partial method hooks that
/// let a composition customize how tracked singletons are released.
/// </summary>
public class CustomDisposeStrategyTests
{
    [Fact]
    public async Task ShouldCallOnDisposePartialMethodWhenDisposing()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}

                               class Dependency : IDependency, IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Default");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Hint(Hint.OnDispose, "On")
                                           .Bind().As(Lifetime.Singleton).To<Dependency>()
                                           .Root<IDependency>("Dep");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var dep = composition.Dep;
                                       composition.Dispose();
                                   }
                               }

                               partial class Composition
                               {
                                   // The OnDispose partial hook is invoked for each
                                   // tracked IDisposable instance prior to its Dispose.
                                   // Returning false tells the composition to proceed
                                   // with the default Dispose() call afterwards.
                                   private partial bool OnDispose<T>(in T disposableInstance)
                                       where T : IDisposable
                                   {
                                       Console.WriteLine("OnDispose invoked");
                                       return false;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["OnDispose invoked", "Default"], result);
    }

    [Fact]
    public async Task ShouldWrapDisposableInstanceViaOnDispose()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ISlowCloser
                               {
                                   void Close();
                               }

                               class SlowCloser : ISlowCloser, IDisposable
                               {
                                   public void Close() => Console.WriteLine("Close");

                                   public void Dispose() => Console.WriteLine("Default");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Hint(Hint.OnDispose, "On")
                                           .Bind().As(Lifetime.Singleton).To<SlowCloser>()
                                           .Root<ISlowCloser>("Dep");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var dep = composition.Dep;
                                       composition.Dispose();
                                   }
                               }

                               partial class Composition
                               {
                                   // The OnDispose partial hook lets the composition
                                   // call Close() instead of Dispose() on tracked
                                   // instances that implement ISlowCloser. Returning
                                   // true tells the composition to skip the default
                                   // Dispose() invocation afterwards.
                                   private partial bool OnDispose<T>(in T disposableInstance)
                                       where T : IDisposable
                                   {
                                       if (disposableInstance is ISlowCloser slowCloser)
                                       {
                                           slowCloser.Close();
                                           return true;
                                       }

                                       return false;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Close"], result);
    }

    [Fact]
    public async Task ShouldNotCallOnDisposeWhenHintIsOff()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}

                               class Dependency : IDependency, IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Default");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       // OnDispose = Off (default)
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Bind().As(Lifetime.Singleton).To<Dependency>()
                                           .Root<IDependency>("Dep");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var dep = composition.Dep;
                                       composition.Dispose();
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Default"], result);
    }

    [Fact]
    public async Task ShouldCallOnDisposeAsyncForAsyncDisposableInstances()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}

                               class Dependency : IDependency, IAsyncDisposable
                               {
                                   public ValueTask DisposeAsync()
                                   {
                                       Console.WriteLine("AsyncDefault");
                                       return ValueTask.CompletedTask;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Hint(Hint.OnDisposeAsync, "On")
                                           .Bind().As(Lifetime.Singleton).To<Dependency>()
                                           .Root<IDependency>("Dep");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var dep = composition.Dep;
                                       composition.DisposeAsync().AsTask().Wait();
                                   }
                               }

                               partial class Composition
                               {
                                   // OnDisposeAsync is the async sibling of OnDispose.
                                   // Returning false from the hook tells the composition
                                   // to proceed with the default DisposeAsync() call.
                                   private partial ValueTask<bool> OnDisposeAsync<T>(in T asyncDisposableInstance)
                                       where T : IAsyncDisposable
                                   {
                                       Console.WriteLine("OnDisposeAsync invoked");
                                       return new ValueTask<bool>(false);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["OnDisposeAsync invoked", "AsyncDefault"], result);
    }

    [Fact]
    public async Task ShouldInvokeOnDisposeForEveryTrackedDisposable()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IFirst {}
                               interface ISecond {}

                               sealed class First : IFirst, IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose-First");
                               }

                               sealed class Second : ISecond, IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose-Second");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Hint(Hint.OnDispose, "On")
                                           .Bind<IFirst>().As(Lifetime.Singleton).To<First>()
                                           .Bind<ISecond>().As(Lifetime.Singleton).To<Second>()
                                           .Root<IFirst>("First")
                                           .Root<ISecond>("Second");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       _ = composition.First;
                                       _ = composition.Second;
                                       composition.Dispose();
                                   }
                               }

                               partial class Composition
                               {
                                   // The hook must be invoked exactly once per tracked
                                   // IDisposable singleton. The generator infers T from the
                                   // switch-case label type (IDisposable), so the concrete
                                   // instance type is observed via GetType().
                                   private partial bool OnDispose<T>(in T disposableInstance)
                                       where T : IDisposable
                                   {
                                       Console.WriteLine($"Hook:{disposableInstance!.GetType().Name}");
                                       return false;
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        // Disposal is LIFO: Second was created last, so it is disposed first.
        result.StdOut.ShouldBe(["Hook:Second", "Dispose-Second", "Hook:First", "Dispose-First"], result);
    }

    [Fact]
    public async Task ShouldForwardExceptionFromDefaultDisposeToOnDisposeException()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency {}

                               sealed class Dependency : IDependency, IDisposable
                               {
                                   public void Dispose() => throw new InvalidOperationException("boom");
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Hint(Hint.OnDispose, "On")
                                           .Bind().As(Lifetime.Singleton).To<Dependency>()
                                           .Root<IDependency>("Dep");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       _ = composition.Dep;
                                       try
                                       {
                                           composition.Dispose();
                                           Console.WriteLine("Dispose-completed");
                                       }
                                       catch (Exception ex)
                                       {
                                           Console.WriteLine($"Propagated:{ex.GetType().Name}");
                                       }
                                   }
                               }

                               partial class Composition
                               {
                                   private partial bool OnDispose<T>(in T disposableInstance)
                                       where T : IDisposable
                                   {
                                       Console.WriteLine("OnDispose invoked");
                                       return false;
                                   }

                                   // The default Dispose() throws. The generated code must
                                   // catch the exception, invoke OnDisposeException with the
                                   // original instance, and continue disposing remaining items
                                   // without re-throwing.
                                   partial void OnDisposeException<T>(T disposableInstance, Exception exception)
                                       where T : IDisposable
                                   {
                                       Console.WriteLine($"OnDisposeException:{exception.GetType().Name}:{exception.Message}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(
            [
                "OnDispose invoked",
                "OnDisposeException:InvalidOperationException:boom",
                "Dispose-completed"
            ],
            result);
    }

    [Fact]
    public async Task ShouldInvokeBothOnDisposeAndOnDisposeAsyncInMixedComposition()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface ISync {}
                               interface IAsync {}

                               sealed class SyncDep : ISync, IDisposable
                               {
                                   public void Dispose() => Console.WriteLine("Dispose-Sync");
                               }

                               sealed class AsyncDep : IAsync, IAsyncDisposable
                               {
                                   public ValueTask DisposeAsync()
                                   {
                                       Console.WriteLine("Dispose-Async");
                                       return default;
                                   }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "Off")
                                           .Hint(Hint.OnDispose, "On")
                                           .Hint(Hint.OnDisposeAsync, "On")
                                           .Bind<ISync>().As(Lifetime.Singleton).To<SyncDep>()
                                           .Bind<IAsync>().As(Lifetime.Singleton).To<AsyncDep>()
                                           .Root<ISync>("SyncRoot")
                                           .Root<IAsync>("AsyncRoot");
                                   }
                               }

                               public class Program
                               {
                                   public static async Task Main()
                                   {
                                       var composition = new Composition();
                                       _ = composition.SyncRoot;
                                       _ = composition.AsyncRoot;
                                       await composition.DisposeAsync();
                                   }
                               }

                               partial class Composition
                               {
                                   private partial bool OnDispose<T>(in T disposableInstance)
                                       where T : IDisposable
                                   {
                                       Console.WriteLine("OnDispose");
                                       return false;
                                   }

                                   private partial ValueTask<bool> OnDisposeAsync<T>(in T asyncDisposableInstance)
                                       where T : IAsyncDisposable
                                   {
                                       Console.WriteLine("OnDisposeAsync");
                                       return new ValueTask<bool>(false);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        // AsyncDep was created last, so DisposeAsync runs first (LIFO).
        result.StdOut.ShouldBe(
            ["OnDisposeAsync", "Dispose-Async", "OnDispose", "Dispose-Sync"],
            result);
    }
}
