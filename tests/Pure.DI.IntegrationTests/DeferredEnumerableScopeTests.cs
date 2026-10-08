namespace Pure.DI.IntegrationTests;

using Core;

public class DeferredEnumerableScopeTests
{
    [Fact]
    public async Task ShouldCreatePerBlockDependenciesWithinEachEnumeration()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IBlock { }
                           sealed class Block : IBlock { }
                           interface IItem { IBlock Block { get; } }
                           sealed class First : IItem
                           {
                               public First(IBlock block) => Block = block;

                               public IBlock Block { get; }
                           }
                           sealed class Second : IItem
                           {
                               public Second(IBlock block) => Block = block;

                               public IBlock Block { get; }
                           }

                           sealed class Consumer
                           {
                               private readonly IBlock _outer;
                               private readonly IEnumerable<IItem> _items;

                               public Consumer(IBlock outer, IEnumerable<IItem> items)
                               {
                                   _outer = outer;
                                   _items = items;
                               }

                               public void Run()
                               {
                                   var first = _items.ToArray();
                                   var second = _items.ToArray();
                                   Console.WriteLine(first.Length == 2 && second.Length == 2);
                                   Console.WriteLine(ReferenceEquals(first[0].Block, first[1].Block));
                                   Console.WriteLine(ReferenceEquals(second[0].Block, second[1].Block));
                                   Console.WriteLine(!ReferenceEquals(_outer, first[0].Block)
                                       && !ReferenceEquals(_outer, second[0].Block)
                                       && !ReferenceEquals(first[0].Block, second[0].Block));
                               }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IBlock>().As(Lifetime.PerBlock).To<Block>()
                                   .Bind<IItem>(Tag.Unique).To<First>()
                                   .Bind<IItem>(Tag.Unique).To<Second>()
                                   .Root<Consumer>("Consumer");
                           }

                           public class Program
                           {
                               public static void Main() => new Composition().Consumer.Run();
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldIsolatePerBlockDependenciesAcrossFactoryAndEnumerableCalls()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IBlock { }
                           sealed class Block : IBlock { }
                           interface IItem { IBlock Block { get; } }
                           sealed class Item : IItem
                           {
                               public Item(IBlock block) => Block = block;

                               public IBlock Block { get; }
                           }

                           sealed class Consumer
                           {
                               private readonly IBlock _outer;
                               private readonly Func<IEnumerable<IItem>> _items;

                               public Consumer(IBlock outer, Func<IEnumerable<IItem>> items)
                               {
                                   _outer = outer;
                                   _items = items;
                               }

                               public void Run()
                               {
                                   var first = _items().Single();
                                   var second = _items().Single();
                                   Console.WriteLine(!ReferenceEquals(_outer, first.Block));
                                   Console.WriteLine(!ReferenceEquals(_outer, second.Block));
                                   Console.WriteLine(!ReferenceEquals(first.Block, second.Block));
                               }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IBlock>().As(Lifetime.PerBlock).To<Block>()
                                   .Bind<IItem>().To<Item>()
                                   .Root<Consumer>("Consumer");
                           }

                           public class Program
                           {
                               public static void Main() => new Composition().Consumer.Run();
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True", "True"], result);
    }

    [Fact]
    public async Task ShouldCreatePerBlockDependenciesWithinEachAsyncEnumeration()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample;

                           interface IBlock { }
                           sealed class Block : IBlock { }
                           interface IItem { IBlock Block { get; } }
                           sealed class Item : IItem
                           {
                               public Item(IBlock block) => Block = block;

                               public IBlock Block { get; }
                           }

                           sealed class Consumer
                           {
                               private readonly IBlock _outer;
                               private readonly IAsyncEnumerable<IItem> _items;

                               public Consumer(IBlock outer, IAsyncEnumerable<IItem> items)
                               {
                                   _outer = outer;
                                   _items = items;
                               }

                               public async Task RunAsync()
                               {
                                   IBlock? first = null;
                                   IBlock? second = null;
                                   await foreach (var item in _items) first = item.Block;
                                   await foreach (var item in _items) second = item.Block;
                                   Console.WriteLine(first is not null && second is not null);
                                   Console.WriteLine(!ReferenceEquals(_outer, first)
                                       && !ReferenceEquals(_outer, second)
                                       && !ReferenceEquals(first, second));
                               }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IBlock>().As(Lifetime.PerBlock).To<Block>()
                                   .Bind<IItem>().To<Item>()
                                   .Root<Consumer>("Consumer");
                           }

                           public class Program
                           {
                               public static async Task Main() => await new Composition().Consumer.RunAsync();
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True", "True"], result);
    }

    [Theory]
    [InlineData("Transient")]
    [InlineData("PerBlock")]
    public async Task ShouldReportCycleThroughAsyncEnumerable(string lifetime)
    {
        var source = """
                     using System;
                     using System.Collections.Generic;
                     using System.Threading.Tasks;
                     using Pure.DI;

                     namespace Sample;

                     interface IClient { bool HasRegistry { get; } }
                     sealed class Client : IClient
                     {
                         private readonly IRegistry _registry;

                         public Client(IRegistry registry) => _registry = registry;

                         public bool HasRegistry => _registry is not null;
                     }

                     interface IRegistry { Task<string> FirstAsync(); }
                     sealed class Registry : IRegistry
                     {
                         private readonly IAsyncEnumerable<IPolicy> _policies;

                         public Registry(IAsyncEnumerable<IPolicy> policies) => _policies = policies;

                         public async Task<string> FirstAsync()
                         {
                             await foreach (var policy in _policies) return policy.Name;
                             return "missing";
                         }
                     }

                     interface IStore { bool HasClient { get; } }
                     sealed class Store : IStore
                     {
                         private readonly IClient _client;

                         public Store(IClient client) => _client = client;

                         public bool HasClient => _client is not null && _client.HasRegistry;
                     }

                     interface IPolicy { string Name { get; } }
                     sealed class Policy : IPolicy
                     {
                         private readonly IStore _store;

                         public Policy(IStore store) => _store = store;

                         public string Name => _store.HasClient ? "ready" : "broken";
                     }

                     sealed class Root
                     {
                         private readonly IClient _client;
                         private readonly IRegistry _registry;

                         public Root(IClient client, IRegistry registry)
                         {
                             _client = client;
                             _registry = registry;
                         }

                         public async Task RunAsync()
                         {
                             Console.WriteLine(_client.HasRegistry);
                             Console.WriteLine(await _registry.FirstAsync());
                         }
                     }

                     partial class Composition
                     {
                         void Setup() => DI.Setup()
                             .DefaultLifetime(Lifetime.###Lifetime###)
                             .Bind<IClient>().To<Client>()
                             .Bind<IRegistry>().To<Registry>()
                             .Bind<IStore>().To<Store>()
                             .Bind<IPolicy>().To<Policy>()
                             .Root<Root>("Root");
                     }

                     public class Program
                     {
                         public static async Task Main() => await new Composition().Root.RunAsync();
                     }
                     """.Replace("###Lifetime###", lifetime);
        var result = await source.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.Count(i => i.Id == LogId.ErrorCyclicDependency).ShouldBe(1, result);
        result.Success.ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldUseEnumerableReplacedByOnNewInstance()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IItem { }
                           sealed class Item : IItem { }
                           sealed class Consumer
                           {
                               private readonly IEnumerable<IItem> _items;

                               public Consumer(IEnumerable<IItem> items) => _items = items;

                               public void Run() => Console.WriteLine(_items.Count());
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Hint(Hint.OnNewInstance, "On")
                                   .Bind<IItem>().To<Item>()
                                   .Root<Consumer>("Consumer");

                               partial void OnNewInstance<T>(ref T value, object? tag, Lifetime lifetime)
                               {
                                   if (value is IEnumerable<IItem>)
                                   {
                                       value = (T)(object)Array.Empty<IItem>();
                                   }
                               }
                           }

                           public class Program
                           {
                               public static void Main() => new Composition().Consumer.Run();
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        result.Errors.ShouldBeEmpty(result);
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["0"], result);
    }
}
