namespace Pure.DI.IntegrationTests;

/// <summary>
/// A shared (Singleton/Scoped) instance first created inside an <c>Owned&lt;T&gt;</c>, see #155:
/// its own <c>Owned&lt;T&gt;</c> and deferred dependencies belong to the shared instance,
/// not to the resolve that happens to create it first.
/// </summary>
public class OwnedInsideSharedInstanceTests
{
    [Theory]
    [InlineData("Singleton", "Owned<Inner>", "() => inner", "On")]
    [InlineData("Singleton", "Owned<Inner>", "() => inner", "Off")]
    [InlineData("Singleton", "Func<Owned<Inner>>", "inner", "On")]
    [InlineData("Singleton", "Func<Owned<Inner>>", "inner", "Off")]
    [InlineData("Singleton", "Lazy<Owned<Inner>>", "() => inner.Value", "On")]
    [InlineData("Scoped", "Owned<Inner>", "() => inner", "On")]
    [InlineData("Scoped", "Func<Owned<Inner>>", "inner", "On")]
    [InlineData("Scoped", "Lazy<Owned<Inner>>", "() => inner.Value", "Off")]
    public async Task ShouldKeepOwnedOfSharedInstanceSeparateFromOwnedThatCreatedIt(string lifetime, string innerType, string getter, string threadSafe)
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
                                       Owned<Dialog> dialog = composition.CreateDialog();
                                       Connection dialogConnection = dialog.Value.Connection;
                                       Service service = dialog.Value.Service;
                                       Owned<Inner> inner = service.GetInner();

                                       dialog.Dispose();
                                       Console.WriteLine(dialogConnection.IsDisposed);
                                       Console.WriteLine(inner.Value.Connection.IsDisposed);

                                       Owned<Inner> innerAfterDialog = service.GetInner();
                                       Console.WriteLine(innerAfterDialog.Value.Connection.IsDisposed);

                                       inner.Dispose();
                                       Console.WriteLine(inner.Value.Connection.IsDisposed);
                                   }
                               }

                               sealed class Connection : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Inner
                               {
                                   public Inner(Connection connection) => Connection = connection;

                                   public Connection Connection { get; }
                               }

                               sealed class Service
                               {
                                   private readonly Func<Owned<Inner>> _getInner;

                                   public Service(#innerType# inner) => _getInner = #getter#;

                                   public Owned<Inner> GetInner() => _getInner();
                               }

                               sealed class Dialog
                               {
                                   public Dialog(Connection connection, Service service)
                                   {
                                       Connection = connection;
                                       Service = service;
                                   }

                                   public Connection Connection { get; }

                                   public Service Service { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "#threadSafe#")
                                           .Bind<Service>().As(#lifetime#).To<Service>()
                                           .Root<Func<Owned<Dialog>>>("CreateDialog");
                               }
                           }
                           """
            .Replace("#lifetime#", lifetime)
            .Replace("#innerType#", innerType)
            .Replace("#getter#", getter)
            .Replace("#threadSafe#", threadSafe)
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False", "False", "True"], result);
    }

    [Theory]
    [InlineData("On")]
    [InlineData("Off")]
    public async Task ShouldKeepOwnedOfSharedInstanceSeparateWhenSharedInstanceIsCreatedByLocalFunction(string threadSafe)
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
                                       Owned<Dialog> dialog = composition.CreateDialog();
                                       Service service = dialog.Value.Left.Service;
                                       Console.WriteLine(ReferenceEquals(service, dialog.Value.Right.Service));

                                       dialog.Dispose();
                                       Console.WriteLine(dialog.Value.Connection.IsDisposed);
                                       Console.WriteLine(service.Inner.Value.Connection.IsDisposed);

                                       service.Inner.Dispose();
                                       Console.WriteLine(service.Inner.Value.Connection.IsDisposed);
                                   }
                               }

                               sealed class Connection : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Inner
                               {
                                   public Inner(Connection connection) => Connection = connection;

                                   public Connection Connection { get; }
                               }

                               class A1 { public A1(A2 a, B2 b) { } }
                               class B1 { public B1(A2 a, B2 b) { } }
                               class A2 { }
                               class B2 { }

                               sealed class Service
                               {
                                   public Service(Owned<Inner> inner, A1 a, B1 b) => Inner = inner;

                                   public Owned<Inner> Inner { get; }
                               }

                               sealed class Left
                               {
                                   public Left(Service service) => Service = service;

                                   public Service Service { get; }
                               }

                               sealed class Right
                               {
                                   public Right(Service service) => Service = service;

                                   public Service Service { get; }
                               }

                               sealed class Dialog
                               {
                                   public Dialog(Connection connection, Left left, Right right)
                                   {
                                       Connection = connection;
                                       Left = left;
                                       Right = right;
                                   }

                                   public Connection Connection { get; }

                                   public Left Left { get; }

                                   public Right Right { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "#threadSafe#")
                                           .Bind<Service>().As(Singleton).To<Service>()
                                           .Bind<A1>().As(Singleton).To<A1>()
                                           .Bind<B1>().As(Singleton).To<B1>()
                                           .Bind<A2>().As(Singleton).To<A2>()
                                           .Bind<B2>().As(Singleton).To<B2>()
                                           .Root<Func<Owned<Dialog>>>("CreateDialog");
                               }
                           }
                           """.Replace("#threadSafe#", threadSafe).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "False", "True"], result);
        result.GeneratedCode.ShouldContain("EnsureServiceExists", Case.Sensitive, result);
    }

    // A shared instance that injects IOwned directly captures the accumulator of the resolve that creates it,
    // as before #155, so it is built in place rather than by an Ensure...Exists() helper declared at the root level.
    [Theory]
    [InlineData("On")]
    [InlineData("Off")]
    public async Task ShouldBuildSharedInstanceInjectingOwnedInPlaceWhenItIsReachedSeveralTimes(string threadSafe)
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
                                       Owned<Dialog> dialog = composition.CreateDialog();
                                       Console.WriteLine(ReferenceEquals(dialog.Value.Left.Service, dialog.Value.Right.Service));
                                       dialog.Dispose();
                                       Owned<Dialog> dialog2 = composition.CreateDialog();
                                       Console.WriteLine(ReferenceEquals(dialog.Value.Left.Service, dialog2.Value.Left.Service));
                                       dialog2.Dispose();
                                   }
                               }

                               class A1 { public A1(A2 a, B2 b) { } }
                               class B1 { public B1(A2 a, B2 b) { } }
                               class A2 { }
                               class B2 { }

                               sealed class Service
                               {
                                   public Service(IOwned owned, A1 a, B1 b) { }
                               }

                               sealed class Left
                               {
                                   public Left(Service service) => Service = service;

                                   public Service Service { get; }
                               }

                               sealed class Right
                               {
                                   public Right(Service service) => Service = service;

                                   public Service Service { get; }
                               }

                               sealed class Dialog
                               {
                                   public Dialog(Left left, Right right)
                                   {
                                       Left = left;
                                       Right = right;
                                   }

                                   public Left Left { get; }

                                   public Right Right { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Hint(Hint.ThreadSafe, "#threadSafe#")
                                           .Bind<Service>().As(Singleton).To<Service>()
                                           .Bind<A1>().As(Singleton).To<A1>()
                                           .Bind<B1>().As(Singleton).To<B1>()
                                           .Bind<A2>().As(Singleton).To<A2>()
                                           .Bind<B2>().As(Singleton).To<B2>()
                                           .Root<Func<Owned<Dialog>>>("CreateDialog");
                               }
                           }
                           """.Replace("#threadSafe#", threadSafe).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
        result.GeneratedCode.ShouldNotContain("EnsureServiceExists", Case.Sensitive, result);
    }

    [Fact]
    public async Task ShouldKeepOwnedOfSharedInstanceSeparateFromOwnedCreatedByFuncWithArgument()
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
                                       Owned<Dialog> dialog = composition.CreateDialog(new Settings("one"));
                                       Service service = dialog.Value.Service;
                                       Console.WriteLine(dialog.Value.Settings.Name);

                                       dialog.Dispose();
                                       Console.WriteLine(dialog.Value.Connection.IsDisposed);
                                       Console.WriteLine(service.Inner.Value.Connection.IsDisposed);

                                       Owned<Dialog> dialog2 = composition.CreateDialog(new Settings("two"));
                                       Console.WriteLine(dialog2.Value.Settings.Name);
                                       Console.WriteLine(ReferenceEquals(service, dialog2.Value.Service));
                                       dialog2.Dispose();
                                       Console.WriteLine(service.Inner.Value.Connection.IsDisposed);
                                   }
                               }

                               sealed class Settings
                               {
                                   public Settings(string name) => Name = name;

                                   public string Name { get; }
                               }

                               sealed class Connection : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Inner
                               {
                                   public Inner(Connection connection) => Connection = connection;

                                   public Connection Connection { get; }
                               }

                               sealed class Service
                               {
                                   public Service(Owned<Inner> inner) => Inner = inner;

                                   public Owned<Inner> Inner { get; }
                               }

                               sealed class Dialog
                               {
                                   public Dialog(Settings settings, Connection connection, Service service)
                                   {
                                       Settings = settings;
                                       Connection = connection;
                                       Service = service;
                                   }

                                   public Settings Settings { get; }

                                   public Connection Connection { get; }

                                   public Service Service { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<Service>().As(Singleton).To<Service>()
                                           .Root<Func<Settings, Owned<Dialog>>>("CreateDialog");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["one", "True", "False", "two", "True", "False"], result);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    public async Task ShouldNotTrackDeferredDependencyOfSharedInstanceInOwnedThatCreatedIt(string lifetime)
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
                                       Owned<Dialog> dialog = composition.CreateDialog();
                                       Service service = dialog.Value.Service;
                                       Connection before = service.CreateConnection();

                                       dialog.Dispose();
                                       Connection after = service.CreateConnection();
                                       Console.WriteLine(before.IsDisposed);
                                       Console.WriteLine(after.IsDisposed);
                                   }
                               }

                               sealed class Connection : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Service
                               {
                                   private readonly Func<Connection> _factory;

                                   public Service(Func<Connection> factory) => _factory = factory;

                                   public Connection CreateConnection() => _factory();
                               }

                               sealed class Dialog
                               {
                                   public Dialog(Service service) => Service = service;

                                   public Service Service { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<Service>().As(#lifetime#).To<Service>()
                                           .Root<Func<Owned<Dialog>>>("CreateDialog");
                               }
                           }
                           """.Replace("#lifetime#", lifetime).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["False", "False"], result);
    }

    [Theory]
    [InlineData("Singleton", "Pure.DI.Owned<Top>", "Create")]
    [InlineData("Scoped", "Pure.DI.Owned<Top>", "Create")]
    [InlineData("Singleton", "Func<Pure.DI.Owned<Top>>", "Create()")]
    public async Task ShouldNotAllocateOwnedWhenOnlySharedInstanceHasDisposableDependencies(string lifetime, string rootType, string call)
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
                                       var top = composition.#call#;
                                       var connection = top.Value.Service.Connection;
                                       top.Dispose();
                                       Console.WriteLine(connection.IsDisposed);
                                   }
                               }

                               sealed class Connection : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Service
                               {
                                   public Service(Connection connection) => Connection = connection;

                                   public Connection Connection { get; }
                               }

                               sealed class Top
                               {
                                   public Top(Service service) => Service = service;

                                   public Service Service { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<Service>().As(#lifetime#).To<Service>()
                                           .Root<#rootType#>("Create");
                               }
                           }
                           """
            .Replace("#lifetime#", lifetime)
            .Replace("#rootType#", rootType)
            .Replace("#call#", call)
            .RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["False"], result);
        result.GeneratedCode.ShouldContain("Pure.DI.Owned.Empty", Case.Sensitive, result);
        result.GeneratedCode.ShouldNotContain("new Pure.DI.Owned(", Case.Sensitive, result);
    }

    [Fact]
    public async Task ShouldCreateOwnAccumulatorForOwnedOfSharedInstance()
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
                                   }
                               }

                               sealed class Connection : IDisposable
                               {
                                   public void Dispose() { }
                               }

                               sealed class Inner
                               {
                                   public Inner(Connection connection) { }
                               }

                               sealed class Service
                               {
                                   public Service(Owned<Inner> inner) { }
                               }

                               sealed class Dialog
                               {
                                   public Dialog(Connection connection, Service service) { }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<Service>().As(Singleton).To<Service>()
                                           .Root<Func<Owned<Dialog>>>("CreateDialog");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        // One accumulator for the dialog and one for the singleton's own Owned<Inner>.
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("new Pure.DI.Owned(")).ShouldBe(2, result);
    }
}
