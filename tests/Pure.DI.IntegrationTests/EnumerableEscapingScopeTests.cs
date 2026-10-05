namespace Pure.DI.IntegrationTests;

/// <summary>
///      Reproduces the "generated enumerable escapes its declaration scope" defect
///      observed in AI.Client with Pure.DI 2.5.4: a transient consumer that takes
///      <c>IEnumerable&lt;T&gt;</c> populated by tagged transient implementations, together with
///      deferred factories in the same graph, produced CS0103 for a <c>perBlock...</c> local
///      variable.
/// </summary>
public class EnumerableEscapingScopeTests
{
    [Fact]
    public async Task ShouldNotEscapeEnumerableLocalScopeForTransientConsumerWithTaggedImplementations()
    {
        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample;

                           interface IMcpServerConnection
                           {
                               string Name { get; }
                           }

                           sealed class StdioConnection : IMcpServerConnection
                           {
                               public string Name => "stdio";
                           }

                           sealed class HttpConnection : IMcpServerConnection
                           {
                               public string Name => "http";
                           }

                           interface IToolSession
                           {
                               string Info { get; }
                           }

                           sealed class ToolSession : IToolSession
                           {
                               private readonly IEnumerable<IMcpServerConnection> _connections;

                               public ToolSession(IEnumerable<IMcpServerConnection> connections) =>
                                   _connections = connections;

                               public string Info =>
                                   string.Join(",", _connections.Select(i => i.Name));
                           }

                           interface ICompositeToolSessionFactory
                           {
                               IToolSession Create();
                           }

                           sealed class CompositeToolSessionFactory : ICompositeToolSessionFactory
                           {
                               private readonly IEnumerable<IMcpServerConnection> _connections;
                               private readonly Func<IToolSession> _toolSessionFactory;

                               public CompositeToolSessionFactory(
                                   IEnumerable<IMcpServerConnection> connections,
                                   Func<IToolSession> toolSessionFactory)
                               {
                                   _connections = connections;
                                   _toolSessionFactory = toolSessionFactory;
                               }

                               public IToolSession Create()
                               {
                                   Console.WriteLine(string.Join(",", _connections.Select(i => i.Name)));
                                   return _toolSessionFactory();
                               }
                           }

                           interface IRunService
                           {
                               string Run();
                           }

                           sealed class RunService : IRunService
                           {
                               private readonly ICompositeToolSessionFactory _factory;

                               public RunService(ICompositeToolSessionFactory factory) => _factory = factory;

                               public string Run() => _factory.Create().Info;
                           }

                           partial class Composition
                           {
                               void Setup() => DI.Setup()
                                   .Bind<IMcpServerConnection>(Tag.Unique).To<StdioConnection>()
                                   .Bind<IMcpServerConnection>(Tag.Unique).To<HttpConnection>()
                                   .Bind<IToolSession>().To<ToolSession>()
                                   .Bind<ICompositeToolSessionFactory>().To<CompositeToolSessionFactory>()
                                   .Bind<IRunService>().As(Lifetime.Singleton).To<RunService>()
                                   .Root<IRunService>("RunService");
                           }

                           public class Program
                           {
                               public static void Main()
                               {
                                   var composition = new Composition();
                                   Console.WriteLine(composition.RunService.Run());
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.Preview));

        // Then
        result.Errors.ShouldBeEmpty();
        result.Success.ShouldBeTrue(result);
    }
}
