#### Schrödinger's cat console application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCat)

This example shows the smallest Pure.DI console application: abstractions, implementations, bindings, and the composition root are kept in one place so the generated object graph is easy to inspect. All code is in [one file](/samples/ShroedingersCat/Program.cs) for easy reading:

> [!TIP]
> The `Setup` method is a compile-time hint for the generator. It is not called at runtime, so it can stay private and contain only composition configuration.

```c#
using System.Diagnostics;
using Pure.DI;
using static Pure.DI.Lifetime;

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

public record CardboardBox<T>(T Content) : IBox<T>;

public class ShroedingersCat(Lazy<State> superposition) : ICat
{
    // The decoherence of the superposition
    // at the time of observation via an irreversible process
    public State State => superposition.Value;

    public override string ToString() => $"{State} cat";
}

// Let's glue all together

partial class Composition
{
    // In fact, this code is never run, and the method can have any name or be a constructor, for example,
    // and can be in any part of the compiled code because this is just a hint to set up an object graph.
    // [Conditional("DI")] attribute avoids generating IL code for the method that follows it,
    // since this method is needed only at compile time.
    [Conditional("DI")]
    static void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "off")
        // Models a random subatomic event that may or may not occur
        .Bind().As(Singleton).To<Random>()
        // Quantum superposition of two states: Alive or Dead
        .Bind().To((Random random) => (State)random.Next(2))
        .Bind().To<ShroedingersCat>()
        // Cardboard box with any contents
        .Bind().To<CardboardBox<TT>>()
        // Provides the composition root
        .Root<Program>("Root");
}

// Time to open boxes!

public class Program(IBox<ICat> box)
{
    // Composition Root, a single place in an application
    // where the composition of the object graphs
    // for an application take place
    public static void Main() => new Composition().Root.Run();

    private void Run() => Console.WriteLine(box);
}
```

The [project file](/samples/ShroedingersCat/ShroedingersCat.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.1">
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
