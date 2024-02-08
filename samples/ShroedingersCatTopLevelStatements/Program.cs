#pragma warning disable CA1050
#pragma warning disable CS8321
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global

using Pure.DI;
using System.Diagnostics;
using static Pure.DI.Lifetime;

// Composition root
new Composition().Root.Run();
return;

// In fact, this code is never run, and the method can have any name or be a constructor, for example,
// and can be in any part of the compiled code because this is just a hint to set up an object graph.
// [Conditional("DI")] attribute avoids generating IL code for the method that follows it.
// Since this method is needed only at the compile time.
[Conditional("DI")]
static void Setup() =>
    DI.Setup(nameof(Composition))
        // Models a random subatomic event that may or may not occur
        .Bind<Random>().As(Singleton).To<Random>()
        // Represents a quantum superposition of 2 states: Alive or Dead
        .Bind<State>().To(ctx =>
        {
            ctx.Inject<Random>(out var random);
            return (State)random.Next(2);
        })
        .Bind<ICat>().To<ShroedingersCat>()
        // Represents a cardboard box with any contents
        .Bind<IBox<TT>>().To<CardboardBox<TT>>()
        // Provides the composition root
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

public class CardboardBox<T>(T content) : IBox<T>
{
    public T Content { get; } = content;

    public override string ToString() => $"[{Content}]";
}

public class ShroedingersCat(Lazy<State> superposition) : ICat
{
    // The decoherence of the superposition
    // at the time of observation via an irreversible process
    public State State => superposition.Value;

    public override string ToString() => $"{State} cat";
}

public partial class Program(IBox<ICat> box)
{
    private void Run() => Console.WriteLine(box);
}

#pragma warning restore CA1050