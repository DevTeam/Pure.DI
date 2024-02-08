using Pure.DI;
using static Pure.DI.Lifetime;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global

namespace Sample;

using System.Diagnostics;

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

// Let's glue all together

internal partial class Composition
{
    // In fact, this code is never run, and the method can have any name or be a constructor, for example,
    // and can be in any part of the compiled code because this is just a hint to set up an object graph.
    // [Conditional("DI")] attribute avoids generating IL code for the method that follows it.
    // Since this method is needed only at the compile time.
    [Conditional("DI")]
    private static void Setup() =>
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
}

// Time to open boxes!

public class Program(IBox<ICat> box)
{
    // Composition Root, a single place in an application
    // where the composition of the object graphs for an application take place
    public static void Main() => new Composition().Root.Run();

    private void Run() => Console.WriteLine(box);
}