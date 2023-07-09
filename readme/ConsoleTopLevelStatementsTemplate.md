#### Top level statements console application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCatTopLevelStatements)

This example is very similar to [simple console application](ConsoleTemplate.md), except that the composition is [defined](/samples/ShroedingersCatTopLevelStatements/Program.cs) as top-level statements and looks a little less verbose:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

// For a top level statements application the name of generated composer is "Composer"
// by default if it was not override in the Setup call below.
new Composition().Root.Run();

// In fact, this code is never run, and the method can have any name or be a constructor, for example,
// and can be in any part of the compiled code because this is just a hint to set up an object graph.
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
```

The [project file](/samples/ShroedingersCatTopLevelStatements/ShroedingersCatTopLevelStatements.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net7.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.x.x"/>
    </ItemGroup>

</Project>
```

Where _2.x.x_ is the latest version of the code generator _Pure.DI_.