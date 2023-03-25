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

        public override string ToString() => $"[{Content}]";
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
        private static void SetupComposition()
        {
            DI.Setup("Composition")
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
                .Root<Program>("Root");
        }
    }

    public class Program
    {
        IBox<ICat> _box;

        internal Program(IBox<ICat> box) => _box = box;

        private void Run() => Console.WriteLine(_box);

        public static void Main()
        {
            using var composition = new Composition();
            composition.Root.Run();
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        (result.StdOut.Contains("[Dead cat]") || result.StdOut.Contains("[Alive cat]")).ShouldBeTrue(result.GeneratedCode);
    }
}