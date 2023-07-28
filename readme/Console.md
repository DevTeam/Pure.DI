#### Console application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCat)

This example demonstrates the creation of a simple console application in the pure DI paradigm using the _Pure.DI_ code generator. All code is in [one file](/samples/ShroedingersCat/Program.cs) for easy perception:

```c#
namespace Sample;
// Let's create an abstraction

public interface IBox<out T>
{
    T Content { get; }
}

public interface ICat
{
    State State { get; }[DebugHelper.cs](..%2Fsrc%2FPure.DI%2FDebugHelper.cs)
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

internal partial class Composition
{
    // In fact, this code is never run, and the method can have any name or be a constructor, for example,
    // and can be in any part of the compiled code because this is just a hint to set up an object graph.
    // Here the setup is part of the generated class, just as an example.
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
            // Represents schrodinger's cat
            .Bind<ICat>().To<ShroedingersCat>()
            // Represents a cardboard box with any content
            .Bind<IBox<TT>>().To<CardboardBox<TT>>()
            // Composition Root
            .Root<Program>("Root");
}

// Time to open boxes!

public class Program
{
    // Composition Root, a single place in an application
    // where the composition of the object graphs for an application take place
    public static void Main() => new Composition().Root.Run();

    private readonly IBox<ICat> _box;

    internal Program(IBox<ICat> box) => _box = box;

    private void Run() => Console.WriteLine(_box);
}
```

The [project file](/samples/ShroedingersCat/ShroedingersCat.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net7.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.0.6">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>
</Project>
```