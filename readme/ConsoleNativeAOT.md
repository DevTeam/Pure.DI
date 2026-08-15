#### Console Native AOT application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCatNativeAOT)

This example shows how the simple console composition can be published as a [native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) application. Pure.DI generates plain C# object creation code, so the dependency graph remains friendly to trimming and ahead-of-time compilation.

> [!TIP]
> Native AOT works best when construction is explicit and reflection-light. Prefer generated roots and bindings over runtime service-location patterns in AOT samples.

The [composition](/samples/ShroedingersCatNativeAOT/Program.cs) uses the same object graph as the [top-level statements console sample](ConsoleTopLevelStatements.md). Pure.DI generates plain `new` chains with no reflection, so the dependency graph is trimming-safe without any extra annotations:

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
        // Represents a quantum superposition of 2 states: Alive or Dead
        .Bind().To((Random random) => (State)random.Next(2))
        .Bind().To<ShroedingersCat>()
        // Represents a cardboard box with any content
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
    public State State => superposition.Value;

    public override string ToString() => $"{State} cat";
}

public partial class Program(IBox<ICat> box)
{
    private void Run() => Console.WriteLine(box);
}
```

> [!NOTE]
> Pure.DI works with native AOT out of the box. The generated code contains no reflection, no dynamic type resolution, and no runtime container — exactly the properties the AOT compiler requires for safe trimming. The only project-level change needed is enabling `<PublishAot>true</PublishAot>` in the `.csproj`.

The [project file](/samples/ShroedingersCatNativeAOT/ShroedingersCatNativeAOT.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <PropertyGroup>
        <PublishAot>true</PublishAot>
        <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.3">
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
