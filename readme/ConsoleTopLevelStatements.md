#### Top level statements console application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCatTopLevelStatements)

This example is very similar to [simple console application](ConsoleTemplate.md), except that the composition is [defined](/samples/ShroedingersCatTopLevelStatements/Program.cs) as top-level statements and looks a little less verbose:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

// Composition root
new Composition().Root.Run();

// In fact, this code is never run, and the method can have any name or be a constructor, for example,
// and can be in any part of the compiled code because this is just a hint to set up an object graph.
DI.Setup("Composition")
    // Models a random subatomic event that may or may not occur
    .Bind().As(Singleton).To<Random>()
    // Represents a quantum superposition of 2 states: Alive or Dead
    .Bind().To((Random random) => (State)random.Next(2))
    // Represents schrodinger's cat
    .Bind().To<ShroedingersCat>()
    // Represents a cardboard box with any content
    .Bind().To<CardboardBox<TT>>()
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
```

The [project file](/samples/ShroedingersCatTopLevelStatements/ShroedingersCatTopLevelStatements.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.38">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|            |                                                                                                 |                                     |
|------------|-------------------------------------------------------------------------------------------------|:------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI Source code generator            |
