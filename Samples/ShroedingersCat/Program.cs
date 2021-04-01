namespace Sample
{
    using System;
    using Pure.DI;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        public static void Main() => Resolver.Resolve<Program>().Run();

        private readonly IBox<ICat> _box;

        internal Program(IBox<ICat> box) => _box = box;

        private void Run() => Console.WriteLine(_box);
    }

    internal static partial class Resolver
    {
        // Models a random subatomic event that may or may not occur
        private static readonly Random Indeterminacy = new();

        static Resolver()
        {
            DI.Setup()
                .Bind<Func<TT>>().To(ctx => new Func<TT>(() => ctx.Resolve<TT>()))
                .Bind<Lazy<TT>>().To<Lazy<TT>>()
                // Represents a quantum superposition of 2 states: Alive or Dead
                .Bind<State>().To(ctx => (State)Indeterminacy.Next(2))
                // Represents schrodinger's cat
                .Bind<ICat>().To<ShroedingersCat>()
                // Represents a cardboard box with any content
                .Bind<IBox<TT>>().To<CardboardBox<TT>>()
                // Composition Root
                .Bind<Program>().To<Program>();
        }
    }

    interface IBox<out T> { T Content { get; } }

    interface ICat { State State { get; } }

    enum State { Alive, Dead }

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
}
