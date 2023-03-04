namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class ShroedingersCatTests
{
    [Fact]
    public async Task ShroedingersCatScenario()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    // Let's create an abstraction

    interface IBox<out T> { T Content { get; } }

    enum State { Alive, Dead }

    interface ICat { State State { get; } }

    // Here is our implementation

    class CardboardBox<T> : IBox<T>
    {
        public CardboardBox(T content) => Content = content;

        public T Content { get; }

        public override string ToString() => $"[{ Content}]";
    }

    class ShroedingersCat : ICat
    {
        // Represents the superposition of the states
        private readonly Lazy<State> _superposition;

        public ShroedingersCat(Lazy<State> superposition) => _superposition = superposition;

        // The decoherence of the superposition at the time of observation via an irreversible process
        public State State => _superposition.Value;

        public override string ToString() => $"{State} cat";
    }

    // Let's glue all together

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                // Models a random subatomic event that may or may not occur
                .Bind<Random>().As(Lifetime.Singleton).To<Random>()
                // Represents a quantum superposition of 2 states: Alive or Dead
                .Bind<State>().To(ctx =>
                {
                    ctx.Inject<Random>(out var random);
                    return (State)random.Next(2);
                }) 
                // Represents schrodinger's cat
                .Bind<ICat>().To<ShroedingersCat>()
                // Represents a cardboard box with any content
                .Bind<IBox<TT>>().To<CardboardBox<TT>>()
                // Composition Root
                .Root<IBox<ICat>>("Cat");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var cat = new Composer().Cat;
            Console.WriteLine(cat);                    
        }
    }                
}
""".RunAsync();

        // Then
        (result.StdOut.Contains("[Dead cat]") || result.StdOut.Contains("[Alive cat]")).ShouldBeTrue(result.GeneratedCode);
    }
}