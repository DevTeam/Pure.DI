/*
$v=true
$p=6
$d=Default lifetime
$h=For example, if some lifetime is used more often than others, you can make it the default lifetime:
$r=Shouldly
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Lifetimes.DefaultLifetimeScenario;

using Xunit;
using static Lifetime;

// {
//# using Pure.DI;
//# using static Pure.DI.Lifetime;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // This hint indicates to not generate methods such as Resolve
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // In real AI apps, the "client" (HTTP handler, connection pool, retries, telemetry)
            // is typically expensive and should be shared.
            //
            // DefaultLifetime(Singleton) makes *all* bindings in this chain singletons,
            // until the chain ends or DefaultLifetime(...) is called again.
            .DefaultLifetime(Singleton)
            .Bind().To<LlmGateway>()
            .Bind().To<RagChatAssistant>()
            .Root<IChatAssistant>("Assistant");

        var composition = new Composition();

        // Think of these as two independent "requests" to resolve the assistant.
        // With singleton lifetime, you get the same assistant instance each time.
        var assistant1 = composition.Assistant;
        var assistant2 = composition.Assistant;

        assistant1.ShouldBe(assistant2);

        // The assistant depends on the same gateway in two places (e.g., chat + embeddings).
        // Because the gateway is singleton, both references are the *same instance*.
        assistant1.ChatGateway.ShouldBe(assistant1.EmbeddingsGateway);

        // And because the assistant itself is singleton, it reuses the same gateway across resolutions.
        assistant1.ChatGateway.ShouldBe(assistant2.ChatGateway);
// }
        composition.SaveClassDiagram();
    }
}

// {
// Represents an "LLM provider gateway": HTTP client, auth, retries, rate limiting, etc.
// NOTE: No secrets here; in real projects you'd configure credentials via secure configuration.
interface ILlmGateway;

// Concrete gateway implementation (placeholder for "OpenAI/Anthropic/Azure/etc. client").
class LlmGateway : ILlmGateway;

// A chat assistant that does RAG (Retrieval-Augmented Generation).
// It needs the gateway for:
// - Chat completions (answer generation)
// - Embeddings (vectorization of question/documents)
interface IChatAssistant
{
    ILlmGateway ChatGateway { get; }

    ILlmGateway EmbeddingsGateway { get; }
}

class RagChatAssistant(
    ILlmGateway chatGateway,
    ILlmGateway embeddingsGateway)
    : IChatAssistant
{
    public ILlmGateway ChatGateway { get; } = chatGateway;

    public ILlmGateway EmbeddingsGateway { get; } = embeddingsGateway;
}
// }