#### Schrödinger's cat console application

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
    // Here the setup is part of the generated class, just as an example.
    private static void Setup() =>
        DI.Setup(nameof(Composition))
            // Models a random subatomic event that may or may not occur
            .Bind().As(Singleton).To<Random>()
            // Represents a quantum superposition of 2 states: Alive or Dead
            .Bind().To(ctx =>
            {
                ctx.Inject<Random>(out var random);
                return (State)random.Next(2);
            })
            // Represents schrodinger's cat
            .Bind().To<ShroedingersCat>()
            // Represents a cardboard box with any content
            .Bind().To<CardboardBox<TT>>()
            // Composition Root
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
```

The [project file](/samples/ShroedingersCat/ShroedingersCat.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.3">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|            |                                                                                                 |                                     |
|------------|-------------------------------------------------------------------------------------------------|:------------------------------------|
| Pure.DI    | [![NuGet](https://buildstats.info/nuget/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI Source code generator            |
