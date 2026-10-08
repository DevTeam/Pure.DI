namespace Pure.DI.IntegrationTests;

/// <summary>
///      Reproduces the "generated enumerable escapes its declaration scope" defect
///      observed in AI.Client with Pure.DI 2.5.4: a transient consumer that takes
///      <c>IEnumerable&lt;T&gt;</c> populated by tagged transient implementations, together with
///      deferred factories in the same graph, produced CS0103 for a <c>perBlock...</c> local
///      variable. The cyclic graph reported in issue #160 is the minimal composition that
///      reaches the same broken scope handling.
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

    [Theory]
    [InlineData("Transient")]
    [InlineData("Singleton")]
    [InlineData("PerResolve")]
    [InlineData("PerBlock")]
    public async Task ShouldNotEscapeEnumerableAndPerBlockLocalsWhenCyclicGraphIsResolvedThroughRoot(string lifetime)
    {
        // Reproduces the issue #160 composition: the graph contains the cycle
        // ChatService -> ChatKindPolicyRegistry -> ScheduledChatKindPolicy -> ChatScheduleStore -> ChatService,
        // and it is resolved through a single root that also consumes the cycle participants.

        // When
        var result = await Graph(lifetime).RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Errors.ShouldBeEmpty(result);
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["scheduled", "scheduled"], result);
    }

    private static string Graph(string lifetime) =>
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using Pure.DI;

        namespace Sample;

        enum ChatKind { Private, Group }

        sealed record ChatThread(Guid Id, ChatKind Kind);

        sealed record ChatDetails(Guid Id, string Behavior);

        interface IChatScheduler { }

        sealed class ChatScheduler : IChatScheduler { }

        interface IChatService
        {
            ChatDetails Get(ChatThread chat);
        }

        sealed class ChatService : IChatService
        {
            private readonly IChatKindPolicyRegistry _policies;

            public ChatService(IChatKindPolicyRegistry policies) => _policies = policies;

            public bool HasRegistry => _policies is not null;

            public ChatDetails Get(ChatThread chat) =>
                new(chat.Id, _policies.Resolve(chat.Kind).Behavior);
        }

        interface IChatKindPolicy
        {
            ChatKind Kind { get; }

            string Behavior { get; }
        }

        interface IChatKindPolicyRegistry
        {
            IChatKindPolicy Resolve(ChatKind kind);
        }

        sealed class ChatKindPolicyRegistry : IChatKindPolicyRegistry
        {
            private readonly IEnumerable<IChatKindPolicy> _policies;

            public ChatKindPolicyRegistry(IEnumerable<IChatKindPolicy> policies) => _policies = policies;

            public IChatKindPolicy Resolve(ChatKind kind) =>
                _policies.First(i => i.Kind == kind);
        }

        sealed class ScheduledChatKindPolicy : IChatKindPolicy
        {
            private readonly IChatScheduleStore _store;
            private readonly Func<IChatScheduler> _scheduler;

            public ScheduledChatKindPolicy(IChatScheduleStore store, Func<IChatScheduler> scheduler)
            {
                _store = store;
                _scheduler = scheduler;
            }

            public ChatKind Kind => ChatKind.Group;

            public string Behavior => _store?.HasChatService == true && _scheduler is not null ? "scheduled" : "broken";
        }

        interface IChatScheduleStore { bool HasChatService { get; } }

        sealed class ChatScheduleStore : IChatScheduleStore
        {
            private readonly IChatService _chats;

            public ChatScheduleStore(IChatService chats) => _chats = chats;

            public bool HasChatService => _chats is ChatService { HasRegistry: true };
        }

        sealed class ChatRunDispatcher
        {
            private readonly IChatService _chats;
            private readonly IChatKindPolicyRegistry _policies;

            public ChatRunDispatcher(IChatService chats, IChatKindPolicyRegistry policies)
            {
                _chats = chats;
                _policies = policies;
            }

            public void WarmUp(ChatThread chat)
            {
                Console.WriteLine(_chats.Get(chat).Behavior);
                Console.WriteLine(_policies.Resolve(chat.Kind).Behavior);
            }
        }

        partial class Composition
        {
            void Setup() => DI.Setup()
                .DefaultLifetime(Lifetime.###Lifetime###)
                .Bind<IChatService>().To<ChatService>()
                .Bind<IChatKindPolicyRegistry>().To<ChatKindPolicyRegistry>()
                .Bind<IChatKindPolicy>().To<ScheduledChatKindPolicy>()
                .Bind<IChatScheduleStore>().To<ChatScheduleStore>()
                .Bind<IChatScheduler>().To<ChatScheduler>()
                .Root<ChatRunDispatcher>("Dispatcher");
        }

        class Program
        {
            static void Main()
            {
                var composition = new Composition();
                composition.Dispatcher.WarmUp(new ChatThread(Guid.NewGuid(), ChatKind.Group));
            }
        }
        """.Replace("###Lifetime###", lifetime);
}
