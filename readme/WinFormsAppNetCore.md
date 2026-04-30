#### WinForms application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WinFormsAppNetCore)

This example shows how to build a modern .NET WinForms application with Pure.DI. The generated composition creates the main form, view models, and infrastructure services without a runtime container.

> [!TIP]
> The main form is created through the `Program` composition root. `Hint.Resolve` is disabled because the application uses explicit roots only.

The composition definition is in the file [Composition.cs](/samples/WinFormsAppNetCore/Composition.cs). Remember to define all the necessary composition roots, for example, this could be a main form such as _FormMain_:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WinFormsAppNetCore;

partial class Composition
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

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

Keep the composition in a `using` block around the WinForms message loop so Pure.DI-owned disposable services are released after the main form exits.

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
        <PackageReference Include="Pure.DI" Version="2.3.6">
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
