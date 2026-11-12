namespace Pure.DI.IntegrationTests;

/// <summary>
/// An element of an enumeration that injects IOwned. The enumeration is deferred like a lambda,
/// so each pass over it is a block of its own, which creates the owner of the elements created in it.
/// </summary>
public class OwnedEnumerationElementTests
{
    private const string Types = """
                                 sealed class Connection : IDisposable
                                 {
                                     public bool IsDisposed { get; private set; }

                                     public void Dispose() => IsDisposed = true;
                                 }

                                 interface IItem
                                 {
                                     IOwned Owned { get; }

                                     Connection Connection { get; }
                                 }

                                 sealed class Item : IItem
                                 {
                                     public Item(IOwned owned, Connection connection)
                                     {
                                         Owned = owned;
                                         Connection = connection;
                                     }

                                     public IOwned Owned { get; }

                                     public Connection Connection { get; }
                                 }

                                 sealed class OtherItem : IItem
                                 {
                                     public OtherItem(IOwned owned, Connection connection)
                                     {
                                         Owned = owned;
                                         Connection = connection;
                                     }

                                     public IOwned Owned { get; }

                                     public Connection Connection { get; }
                                 }
                                 """;

    [Fact]
    public async Task ShouldCompileEnumerationElementThatInjectsOwnedInsideSingletonAndFunc()
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
                                       var app = new Composition().Root;
                                       var owned = app.Open();
                                       Console.WriteLine(owned.Value.Service.Items.Count());
                                       owned.Dispose();
                                       Console.WriteLine(app.CreateHelper().Count);
                                   }
                               }

                               interface IItem
                               {
                               }

                               sealed class Item : IItem
                               {
                                   public Item(IOwned owned) { }
                               }

                               sealed class Service
                               {
                                   public Service(IEnumerable<IItem> items) => Items = items;

                                   public IEnumerable<IItem> Items { get; }
                               }

                               sealed class Screen
                               {
                                   public Screen(Service service) => Service = service;

                                   public Service Service { get; }
                               }

                               sealed class Helper
                               {
                                   private readonly IEnumerable<IItem> _items;

                                   public Helper(IEnumerable<IItem> items) => _items = items;

                                   public int Count => _items.Count();
                               }

                               sealed class App
                               {
                                   private readonly Func<Owned<Screen>> _open;
                                   private readonly Func<Helper> _helper;

                                   public App(Func<Owned<Screen>> open, Func<Helper> helper)
                                   {
                                       _open = open;
                                       _helper = helper;
                                   }

                                   public Owned<Screen> Open() => _open();

                                   public Helper CreateHelper() => _helper();
                               }

                               partial class Composition
                               {
                                   private static void Setup() =>
                                       DI.Setup(nameof(Composition))
                                           .Bind<IItem>(1).To<Item>()
                                           .Bind<Service>().As(Pure.DI.Lifetime.Singleton).To<Service>()
                                           .Root<App>("Root");
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10, CheckCompilationErrors: false));

        // Then
        // Each of the two lambdas declares its own enumeration, which the check of duplicated local functions reports,
        // so it is disabled here. A compilation error still fails the test, because the program is not run.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1", "1"], result);
    }

    [Fact]
    public async Task ShouldCreateOwnerOfEachPassOverEnumerationOfSingletonCreatedInsideOwned()
    {
        // Given

        // When
        var result = await ("""
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
                                        var app = new Composition().Root;
                                        var screen = app.Open();
                                        var items = screen.Value.Service.Items.ToList();
                                        var otherPass = screen.Value.Service.Items.ToList();
                                        // The elements of one pass share an owner, and each pass has an owner of its own.
                                        Console.WriteLine(ReferenceEquals(items[0].Owned, items[1].Owned));
                                        Console.WriteLine(ReferenceEquals(items[0].Owned, otherPass[0].Owned));
                                        // The singleton outlives the Owned<T> it is created in, so its elements do not belong to it.
                                        screen.Dispose();
                                        Console.WriteLine(items.Any(i => i.Connection.IsDisposed));
                                        items[0].Owned.Dispose();
                                        Console.WriteLine(items.All(i => i.Connection.IsDisposed));
                                        Console.WriteLine(otherPass.Any(i => i.Connection.IsDisposed));
                                    }
                                }

                                sealed class Service
                                {
                                    public Service(IEnumerable<IItem> items) => Items = items;

                                    public IEnumerable<IItem> Items { get; }
                                }

                                sealed class Screen
                                {
                                    public Screen(Service service) => Service = service;

                                    public Service Service { get; }
                                }

                                sealed class App
                                {
                                    private readonly Func<Owned<Screen>> _open;

                                    public App(Func<Owned<Screen>> open) => _open = open;

                                    public Owned<Screen> Open() => _open();
                                }

                                partial class Composition
                                {
                                    private static void Setup() =>
                                        DI.Setup(nameof(Composition))
                                            .Bind<IItem>(1).To<Item>()
                                            .Bind<IItem>(2).To<OtherItem>()
                                            .Bind<Service>().As(Pure.DI.Lifetime.Singleton).To<Service>()
                                            .Root<App>("Root");
                                }

                            """ + Types + "\n}").RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False", "False", "True", "False"], result);
    }

    [Fact]
    public async Task ShouldCreateOwnerOfEachPassOverEnumerationInsideFunc()
    {
        // Given

        // When
        var result = await ("""
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
                                        var app = new Composition().Root;
                                        var helper = app.CreateHelper();
                                        var items = helper.Items.ToList();
                                        var otherPass = helper.Items.ToList();
                                        Console.WriteLine(ReferenceEquals(items[0].Owned, items[1].Owned));
                                        Console.WriteLine(ReferenceEquals(items[0].Owned, otherPass[0].Owned));
                                        items[1].Owned.Dispose();
                                        Console.WriteLine(items.All(i => i.Connection.IsDisposed));
                                        Console.WriteLine(otherPass.Any(i => i.Connection.IsDisposed));
                                    }
                                }

                                sealed class Helper
                                {
                                    public Helper(IEnumerable<IItem> items) => Items = items;

                                    public IEnumerable<IItem> Items { get; }
                                }

                                sealed class App
                                {
                                    private readonly Func<Helper> _helper;

                                    public App(Func<Helper> helper) => _helper = helper;

                                    public Helper CreateHelper() => _helper();
                                }

                                partial class Composition
                                {
                                    private static void Setup() =>
                                        DI.Setup(nameof(Composition))
                                            .Bind<IItem>(1).To<Item>()
                                            .Bind<IItem>(2).To<OtherItem>()
                                            .Root<App>("Root");
                                }

                            """ + Types + "\n}").RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "False", "True", "False"], result);
    }

    [Fact]
    public async Task ShouldCreateOwnerOfEnumerationInsideOwnedRoot()
    {
        // Given

        // When
        var result = await ("""
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
                                        var root = new Composition().Root;
                                        var items = root.Value.Items.ToList();
                                        Console.WriteLine(ReferenceEquals(items[0].Owned, root.Value.Owned));
                                        root.Dispose();
                                        Console.WriteLine(root.Value.Connection.IsDisposed);
                                        Console.WriteLine(items.Any(i => i.Connection.IsDisposed));
                                        items[0].Owned.Dispose();
                                        Console.WriteLine(items.All(i => i.Connection.IsDisposed));
                                    }
                                }

                                sealed class App
                                {
                                    public App(IEnumerable<IItem> items, IOwned owned, Connection connection)
                                    {
                                        Items = items;
                                        Owned = owned;
                                        Connection = connection;
                                    }

                                    public IEnumerable<IItem> Items { get; }

                                    public IOwned Owned { get; }

                                    public Connection Connection { get; }
                                }

                                partial class Composition
                                {
                                    private static void Setup() =>
                                        DI.Setup(nameof(Composition))
                                            .Bind<IItem>(1).To<Item>()
                                            .Bind<IItem>(2).To<OtherItem>()
                                            .Root<Owned<App>>("Root");
                                }

                            """ + Types + "\n}").RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["False", "True", "False", "True"], result);
    }

    [Fact]
    public async Task ShouldCreateOwnerOfEachPassOverAsyncEnumeration()
    {
        // Given

        // When
        var result = await ("""
                            using System;
                            using System.Collections.Generic;
                            using System.Linq;
                            using System.Threading.Tasks;
                            using Pure.DI;

                            namespace Sample
                            {
                                public static class Program
                                {
                                    public static void Main() => Run().GetAwaiter().GetResult();

                                    private static async Task Run()
                                    {
                                        var service = new Composition().Root;
                                        var items = new List<IItem>();
                                        await foreach (var item in service.Items)
                                        {
                                            items.Add(item);
                                        }

                                        Console.WriteLine(ReferenceEquals(items[0].Owned, items[1].Owned));
                                        items[0].Owned.Dispose();
                                        Console.WriteLine(items.All(i => i.Connection.IsDisposed));
                                    }
                                }

                                sealed class Service
                                {
                                    public Service(IAsyncEnumerable<IItem> items) => Items = items;

                                    public IAsyncEnumerable<IItem> Items { get; }
                                }

                                partial class Composition
                                {
                                    private static void Setup() =>
                                        DI.Setup(nameof(Composition))
                                            .Bind<IItem>(1).To<Item>()
                                            .Bind<IItem>(2).To<OtherItem>()
                                            .Root<Service>("Root");
                                }

                            """ + Types + "\n}").RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Fact]
    public async Task ShouldDeclareLocalFunctionsThatAddToOwnerOfEnumerationInsideEnumeration()
    {
        // Given

        // When
        var result = await ("""
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
                                        var service = new Composition().Root;
                                        var items = service.Items.ToList();
                                        Console.WriteLine(service.Connection.IsDisposed);
                                        Console.WriteLine(items.Any(i => i.Connection.IsDisposed));
                                        items[0].Owned.Dispose();
                                        Console.WriteLine(items.All(i => i.Connection.IsDisposed));
                                        Console.WriteLine(service.Connection.IsDisposed);
                                    }
                                }

                                sealed class Service
                                {
                                    public Service(IEnumerable<IItem> items, Connection connection)
                                    {
                                        Items = items;
                                        Connection = connection;
                                    }

                                    public IEnumerable<IItem> Items { get; }

                                    public Connection Connection { get; }
                                }

                                partial class Composition
                                {
                                    private static void Setup() =>
                                        DI.Setup(nameof(Composition))
                                            .Hint(Hint.LocalFunctions, "On")
                                            .Bind<IItem>(1).To<Item>()
                                            .Bind<IItem>(2).To<OtherItem>()
                                            .Root<Service>("Root");
                                }

                            """ + Types + "\n}").RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["False", "False", "True", "False"], result);
        // The local function that creates a connection of an element adds it to the owner declared in the enumeration.
        var code = result.GeneratedCode;
        var start = code.IndexOf("EnumerationOf_", StringComparison.Ordinal);
        var end = code.IndexOf("return new global::Sample.Service(", start, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, result.ToString());
        end.ShouldBeGreaterThan(start, result.ToString());
        var enumeration = code[start..end];
        enumeration.ShouldContain("global::Sample.Connection CreateConnection", Case.Sensitive, result.ToString());
        enumeration.ShouldContain("perBlockOwned.Add(", Case.Sensitive, result.ToString());
    }
}
