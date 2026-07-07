#### Top-level statements console application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCatTopLevelStatements)

This example shows the same object graph as the [simple console application](ConsolePageTemplate.md), but defines the composition directly in [Program.cs](/samples/ShroedingersCatTopLevelStatements/Program.cs) with top-level statements. It keeps the setup compact while still producing the same compile-time validated composition:

> [!TIP]
> Top-level statements are convenient for small samples. For larger applications, move setup into a partial composition class so roots, lifetimes, and infrastructure bindings are easier to navigate.

```c#
using System.Diagnostics;
using Pure.DI;
using static Pure.DI.Lifetime;

// Composition root
new Composition().Root.Run();
return;

// In fact, this code is never run, and the method can have any name or be a constructor, for example,
// and can be in any part of the compiled code because this is just a hint to set up an object graph.
// [Conditional("DI")] attribute avoids generating IL code for the method that follows it,
// since this method is needed only at compile time.
[Conditional("DI")]
static void Setup() =>
    DI.Setup(nameof(Composition))
        // Models a random subatomic event that may or may not occur
        .Bind().As(Singleton).To<Random>()
        // Quantum superposition of two states: Alive or Dead
        .Bind().To((Random random) => (State)random.Next(2))
        .Bind().To<ShroedingersCat>()
        // Cardboard box with any contents
        .Bind().To<CardboardBox<TT>>()
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

public record CardboardBox<T>(T Content) : IBox<T>;

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
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>
```

It contains an additional reference to the NuGet package:

|         |                                                                                            |                          |
|---------|--------------------------------------------------------------------------------------------|:-------------------------|
| Pure.DI | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator |
