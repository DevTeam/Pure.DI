namespace Sample
{
    using System;
    using Pure.DI;
    using static Pure.DI.Lifetime;

// Let's create an abstraction

    public interface IBox<out T>
    {
        T Content { get; }
    }

    public interface ICat
    {
        State State { get; }
    }

    public enum State
    {
        Alive,
        Dead
    }

// Here is our implementation

    public class CardboardBox<T> : IBox<T>
    {
        public CardboardBox(T content) => Content = content;

        public T Content { get; }

        public override string ToString() => $"[{Content}]";
    }

    public class ShroedingersCat : ICat
    {
        // Represents the superposition of the states
        private readonly Lazy<State> _superposition;

        public ShroedingersCat(Lazy<State> superposition) => _superposition = superposition;

        // The decoherence of the superposition at the time of observation via an irreversible process
        public State State => _superposition.Value;

        public override string ToString() => $"{State} cat";
    }

// Let's glue all together

    public static partial class Composer
    {
        // Actually, this code never runs and the method might have any name or be a constructor for instance
        // because this is just a hint to set up an object graph.
        private static void Setup() =>
            DI.Setup()
                // Models a random subatomic event that may or may not occur
                .Bind<Random>().As(Singleton).To<Random>()
                // Represents a quantum superposition of 2 states: Alive or Dead
                .Bind<State>().To(ctx => (State)ctx.Resolve<Random>().Next(2))
                // Represents schrodinger's cat
                .Bind<ICat>().To<ShroedingersCat>()
                // Represents a cardboard box with any content
                .Bind<IBox<TT>>().To<CardboardBox<TT>>()
                // Composition Root
                .Bind<Program>().To<Program>();
    }

// Time to open boxes!
    public class Program
    {
        // Composition Root, a single place in an application
        // where the composition of the object graphs for an application take place
        public static void Main() => Composer.ResolveProgram().Run();

        private readonly IBox<ICat> _box;

        internal Program(IBox<ICat> box) => _box = box;

        private void Run() => Console.WriteLine(_box);
    }
}