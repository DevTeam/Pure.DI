#pragma warning disable CA1050
// ReSharper disable UnusedMember.Global

using Pure.DI;
using static Pure.DI.Lifetime;

// For a top level statements application the name of generated composer is "Composer"
// by default if it was not override in the Setup call below.
new Composition().Root.Run();

DI.Setup("Composition")
    // Models a random subatomic event that may or may not occur
    .Bind<Random>().As(Singleton).To<Random>()
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
    // Here it is equivalent to Bind<Program>().To<Program>()
    .Root<Program>("Root");

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

public partial class Program
{
    private readonly IBox<ICat> _box;

    internal Program(IBox<ICat> box) => _box = box;

    private void Run() => Console.WriteLine(_box);
}

#pragma warning restore CA1050