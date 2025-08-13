#### WinForms application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WinFormsAppNetCore)

This example demonstrates the creation of a WinForms application in the pure DI paradigm using the Pure.DI code generator.

The composition definition is in the file [Composition.cs](/samples/WinFormsAppNetCore/Composition.cs). Remember to define all the necessary roots of the composition, for example, this could be a main form such as _FormMain_:

```csharp
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WinFormsAppNetCore;

partial class Composition
{
    private void Setup() => DI.Setup()
        .Root<Program>(nameof(Root))

        .Bind().As(Singleton).To<FormMain>()
        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WinFormsDispatcher>();
}
```

A single instance of the _Composition_ class is defined in the _Main_ method of the [Program.cs](/samples/WinFormsAppNetCore/Program.cs) file:

```c#
namespace WinFormsAppNetCore;

public class Program(FormMain formMain)
{
    [STAThread]
    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var composition = new Composition();
        composition.Root.Run();
    }

    private void Run() => Application.Run(formMain);
}
```

The [project file](/samples/WinFormsAppNetCore/WinFormsAppNetCore.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.3">
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
