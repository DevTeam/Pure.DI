namespace Pure.DI.IntegrationTests;

public class DeferredEnumerableFactoryCycleTests
{
    [Fact]
    public async Task ShouldGenerateNestedEnumerablesWithAnActiveFactoryCycle()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IConnection { IToolHost Host { get; } }
                           interface IToolHost { IEnumerable<ITool> Tools { get; } }
                           interface ITool { Func<IFactory> Factory { get; } }
                           interface IFactory { IEnumerable<IConnection> Connections { get; } }

                           sealed class Connection(IToolHost host) : IConnection
                           {
                               public IToolHost Host { get; } = host;
                           }

                           sealed class ToolHost(IEnumerable<ITool> tools) : IToolHost
                           {
                               public IEnumerable<ITool> Tools { get; } = tools;
                           }

                           sealed class Tool(Func<IFactory> factory) : ITool
                           {
                               public Func<IFactory> Factory { get; } = factory;
                           }

                           sealed class Factory(IEnumerable<IConnection> connections) : IFactory
                           {
                               public IEnumerable<IConnection> Connections { get; } = connections;
                           }

                           sealed class Root(IEnumerable<IConnection> connections)
                           {
                               public void Run()
                               {
                                   var tool = connections.Single().Host.Tools.Single();
                                   Console.WriteLine(tool.Factory().Connections.Single().Host.Tools.Single() is not null);
                               }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IConnection>(Tag.Unique).To<Connection>()
                                   .Bind<IToolHost>().To<ToolHost>()
                                   .Bind<ITool>(Tag.Unique).To<Tool>()
                                   .Bind<IFactory>().To<Factory>()
                                   .Root<Root>("Root");
                           }

                           public class Program
                           {
                               public static void Main() => new Composition().Root.Run();
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        result.Errors.ShouldBeEmpty(result);
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }

    [Fact]
    public async Task ShouldGenerateNestedAsyncEnumerablesWithAnActiveFactoryCycle()
    {
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Threading.Tasks;
                           using Pure.DI;

                           namespace Sample;

                           interface IConnection { IToolHost Host { get; } }
                           interface IToolHost { IAsyncEnumerable<ITool> Tools { get; } }
                           interface ITool { Func<IFactory> Factory { get; } }
                           interface IFactory { IAsyncEnumerable<IConnection> Connections { get; } }

                           sealed class Connection(IToolHost host) : IConnection
                           {
                               public IToolHost Host { get; } = host;
                           }

                           sealed class ToolHost(IAsyncEnumerable<ITool> tools) : IToolHost
                           {
                               public IAsyncEnumerable<ITool> Tools { get; } = tools;
                           }

                           sealed class Tool(Func<IFactory> factory) : ITool
                           {
                               public Func<IFactory> Factory { get; } = factory;
                           }

                           sealed class Factory(IAsyncEnumerable<IConnection> connections) : IFactory
                           {
                               public IAsyncEnumerable<IConnection> Connections { get; } = connections;
                           }

                           sealed class Root(IAsyncEnumerable<IConnection> connections)
                           {
                               public async Task RunAsync()
                               {
                                   await foreach (var connection in connections)
                                   {
                                       await foreach (var tool in connection.Host.Tools)
                                       {
                                           await foreach (var nested in tool.Factory().Connections)
                                           {
                                               Console.WriteLine(nested.Host is not null);
                                               return;
                                           }
                                       }
                                   }
                               }
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IConnection>(Tag.Unique).To<Connection>()
                                   .Bind<IToolHost>().To<ToolHost>()
                                   .Bind<ITool>(Tag.Unique).To<Tool>()
                                   .Bind<IFactory>().To<Factory>()
                                   .Root<Root>("Root");
                           }

                           public class Program
                           {
                               public static async Task Main() => await new Composition().Root.RunAsync();
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp12));

        result.Errors.ShouldBeEmpty(result);
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["True"], result);
    }
}
