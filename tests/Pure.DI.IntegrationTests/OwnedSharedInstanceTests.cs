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
}
