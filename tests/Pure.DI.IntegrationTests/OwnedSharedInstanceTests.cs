namespace Pure.DI.IntegrationTests;

/// <summary>
/// Shared (Singleton/Scoped) instances first created inside an <c>Owned&lt;T&gt;</c> or a <c>Func&lt;TArg, T&gt;</c>, see #155.
/// </summary>
public class OwnedSharedInstanceTests
{
    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    public async Task ShouldNotDisposeDependencyOfSharedInstanceWithOwnedThatCreatedIt(string lifetime)
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
                                       Func<Owned<Dialog>> createDialog = composition.CreateDialog;
                                       Owned<Dialog> dialog = createDialog();
                                       Service service = dialog.Value.Service;

                                       dialog.Dispose();
                                       Console.WriteLine(service.Connection.IsDisposed);
                                       Console.WriteLine(ReferenceEquals(service, composition.Service));
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

                               sealed class Dialog
                               {
                                   public Dialog(Service service) => Service = service;

                                   public Service Service { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<Connection>().To<Connection>()
                                           .Bind<Service>().As(#lifetime#).To<Service>()
                                           .Root<Func<Owned<Dialog>>>("CreateDialog")
                                           .Root<Service>("Service");
                               }
                           }
                           """.Replace("#lifetime#", lifetime).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["False", "True"], result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShouldNotDisposeDependencyOfSingletonWithOwnedRegardlessOfResolutionOrder(bool resolveSingletonFirst)
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
                                       if (bool.Parse("#resolveSingletonFirst#"))
                                       {
                                           _ = composition.Service;
                                       }

                                       Owned<Dialog> dialog = composition.CreateDialog();
                                       Service service = dialog.Value.Service;
                                       dialog.Dispose();
                                       Console.WriteLine(service.Connection.IsDisposed);
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

                               sealed class Dialog
                               {
                                   public Dialog(Service service) => Service = service;

                                   public Service Service { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<Connection>().To<Connection>()
                                           .Bind<Service>().As(Singleton).To<Service>()
                                           .Root<Func<Owned<Dialog>>>("CreateDialog")
                                           .Root<Service>("Service");
                               }
                           }
                           """.Replace("#resolveSingletonFirst#", resolveSingletonFirst.ToString()).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["False"], result);
    }

    [Theory]
    [InlineData("""
                .Bind<Settings>().To<Settings>()
                .Root<Func<Top>>("Create");
                """)]
    [InlineData("""
                .Hint(Hint.Resolve, "Off")
                .RootArg<Settings>("settings")
                .Root<Top>("Create");
                """)]
    [InlineData("""
                .Bind<Settings>().To<Settings>()
                .Root<Func<Owned<Top>>>("Create");
                """)]
    [InlineData("""
                .Root<Func<Settings, Top>>("Create");
                """)]
    public async Task ShouldCreateSharedInstanceInOnePlaceRegardlessOfRootKind(string roots)
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

                               // Each level has two singletons that both depend on both singletons of the next level.
                               class A1 { public A1(A2 a, B2 b) { } }
                               class B1 { public B1(A2 a, B2 b) { } }
                               class A2 { public A2(A3 a, B3 b) { } }
                               class B2 { public B2(A3 a, B3 b) { } }
                               class A3 { public A3(A4 a, B4 b) { } }
                               class B3 { public B3(A4 a, B4 b) { } }
                               class A4 { }
                               class B4 { }

                               class Settings { }
                               class Top { public Top(A1 a, B1 b, Settings settings) { } }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<A1>().As(Singleton).To<A1>()
                                           .Bind<B1>().As(Singleton).To<B1>()
                                           .Bind<A2>().As(Singleton).To<A2>()
                                           .Bind<B2>().As(Singleton).To<B2>()
                                           .Bind<A3>().As(Singleton).To<A3>()
                                           .Bind<B3>().As(Singleton).To<B3>()
                                           .Bind<A4>().As(Singleton).To<A4>()
                                           .Bind<B4>().As(Singleton).To<B4>()
                                           #roots#
                               }
                           }
                           """.Replace("#roots#", roots).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.GeneratedCode.Split(Environment.NewLine).Count(i => i.Contains("new global::Sample.A4()")).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldDisposeTransientCreatedDirectlyUnderOwnedNextToSharedInstance()
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
                                       Connection ownConnection = dialog.Value.Connection;
                                       Connection serviceConnection = dialog.Value.Service.Connection;
                                       dialog.Dispose();
                                       Console.WriteLine(ownConnection.IsDisposed);
                                       Console.WriteLine(serviceConnection.IsDisposed);
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
                                           .Bind<Connection>().To<Connection>()
                                           .Bind<Service>().As(Singleton).To<Service>()
                                           .Root<Func<Owned<Dialog>>>("CreateDialog");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False"], result);
    }

    [Fact]
    public async Task ShouldKeepSingletonAccumulatorWhenSharedInstanceIsCreatedInsideOwned()
    {
        // Given

        // When
        var result = await """
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
                                       var composition = new Composition();
                                       Owned<Dialog> dialog = composition.CreateDialog();
                                       Console.WriteLine(string.Join(",", dialog.Value.Registry.Select(i => i.GetType().Name)));
                                   }
                               }

                               sealed class Registry : List<IDisposable> { }

                               sealed class Cache : IDisposable
                               {
                                   public void Dispose() { }
                               }

                               sealed class Service : IDisposable
                               {
                                   public Service(Cache cache) { }

                                   public void Dispose() { }
                               }

                               sealed class Dialog
                               {
                                   public Dialog(Service service, Registry registry) => Registry = registry;

                                   public Registry Registry { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Accumulate<IDisposable, Registry>(Singleton)
                                           .Bind<Cache>().As(Singleton).To<Cache>()
                                           .Bind<Service>().As(Singleton).To<Service>()
                                           .Root<Func<Owned<Dialog>>>("CreateDialog");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["Cache,Service"], result);
    }

    [Fact]
    public async Task ShouldGiveOwnedCreatedBySharedInstanceItsOwnAccumulatorWhenSharedInstanceIsFirstCreatedInsideOwned()
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
                                       Owned<Dialog> dialog = composition.Top.CreateDialog();
                                       Owned<Proxy> proxy = dialog.Value.Config.CreateProxy();
                                       dialog.Dispose();
                                       Console.WriteLine(proxy.Value.IsDisposed);
                                       proxy.Dispose();
                                       Console.WriteLine(proxy.Value.IsDisposed);
                                       Console.WriteLine(ReferenceEquals(dialog.Value.Config, dialog.Value.Other.Config));
                                   }
                               }

                               sealed class Proxy : IDisposable
                               {
                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               sealed class Connection : IDisposable
                               {
                                   public void Dispose() { }
                               }

                               sealed class ConfigProvider
                               {
                                   public ConfigProvider(Func<Owned<Proxy>> createProxy) => CreateProxy = createProxy;

                                   public Func<Owned<Proxy>> CreateProxy { get; }
                               }

                               sealed class Other
                               {
                                   public Other(ConfigProvider config) => Config = config;

                                   public ConfigProvider Config { get; }
                               }

                               sealed class Dialog
                               {
                                   public Dialog(Connection connection, ConfigProvider config, Other other)
                                   {
                                       Config = config;
                                       Other = other;
                                   }

                                   public ConfigProvider Config { get; }

                                   public Other Other { get; }
                               }

                               sealed class Top
                               {
                                   public Top(Func<Owned<Dialog>> createDialog) => CreateDialog = createDialog;

                                   public Func<Owned<Dialog>> CreateDialog { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<ConfigProvider>().As(Singleton).To<ConfigProvider>()
                                           .Root<Top>("Top");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        // The singleton's Ensure...Exists() helper builds the Func<Owned<Proxy>>, so that Owned must not
        // reuse the accumulator of the Owned<Dialog> the singleton was first created in.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["False", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldCompileSharedInstanceThatInjectsOwnedAccumulatorWhenFirstCreatedInsideOwned()
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
                                       Owned<Dialog> dialog = composition.Top.CreateDialog();
                                       Console.WriteLine(ReferenceEquals(dialog.Value.Holder, dialog.Value.Other.Holder));
                                       dialog.Dispose();
                                   }
                               }

                               sealed class Connection : IDisposable
                               {
                                   public void Dispose() { }
                               }

                               sealed class OwnedHolder
                               {
                                   public OwnedHolder(IOwned owned, Connection connection) => Owned = owned;

                                   public IOwned Owned { get; }
                               }

                               sealed class Other
                               {
                                   public Other(OwnedHolder holder) => Holder = holder;

                                   public OwnedHolder Holder { get; }
                               }

                               sealed class Dialog
                               {
                                   public Dialog(OwnedHolder holder, Other other)
                                   {
                                       Holder = holder;
                                       Other = other;
                                   }

                                   public OwnedHolder Holder { get; }

                                   public Other Other { get; }
                               }

                               sealed class Top
                               {
                                   public Top(Func<Owned<Dialog>> createDialog) => CreateDialog = createDialog;

                                   public Func<Owned<Dialog>> CreateDialog { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<OwnedHolder>().As(Singleton).To<OwnedHolder>()
                                           .Root<Top>("Top");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        // The singleton injects the accumulator of the Owned<Dialog> lambda it is first created in,
        // which an Ensure...Exists() helper declared outside that lambda cannot see.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldNotCollectTransientDependencyOfSingletonWithTransientAccumulator()
    {
        // Given

        // When
        var result = await """
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
                                       var composition = new Composition();
                                       var (dialog, registry) = composition.Root;
                                       Console.WriteLine(registry.Count);
                                       Console.WriteLine(ReferenceEquals(registry[0], dialog.Connection));
                                   }
                               }

                               sealed class Registry : List<IDisposable> { }

                               sealed class Connection : IDisposable
                               {
                                   public void Dispose() { }
                               }

                               sealed class Service
                               {
                                   public Service(Connection connection) { }
                               }

                               sealed class Dialog
                               {
                                   public Dialog(Connection connection, Service service) => Connection = connection;

                                   public Connection Connection { get; }
                               }

                               partial class Composition
                               {
                                   private void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Accumulate<IDisposable, Registry>(Transient)
                                           .Bind<Connection>().To<Connection>()
                                           .Bind<Service>().As(Singleton).To<Service>()
                                           .Root<(Dialog dialog, Registry registry)>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        // A singleton's transient dependencies belong to the singleton, not to the resolve that first created it.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "True"], result);
    }

    // A PerResolve instance captured by a singleton belongs to the singleton (a captive dependency),
    // so the Owned that happens to create it does not dispose it, whatever the constructor argument order.
    // Before #155 was fixed, the Owned disposed an instance the singleton still uses.
    [Theory]
    [InlineData("Connection connection, Service service")]
    [InlineData("Service service, Connection connection")]
    public async Task ShouldNotDisposePerResolveDependencyCapturedBySingletonWithOwned(string dialogParameters)
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
                                       Connection connection = dialog.Value.Connection;
                                       Console.WriteLine(ReferenceEquals(connection, dialog.Value.Service.Connection));
                                       dialog.Dispose();
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

                               sealed class Dialog
                               {
                                   public Dialog(#dialogParameters#)
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
                                           .Bind<Connection>().As(PerResolve).To<Connection>()
                                           .Bind<Service>().As(Singleton).To<Service>()
                                           .Root<Func<Owned<Dialog>>>("CreateDialog");
                               }
                           }
                           """.Replace("#dialogParameters#", dialogParameters).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False"], result);
    }
}
