#### WinForms application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WinFormsApp)

This example demonstrates the creation of a WinForms application in the pure DI paradigm using the _Pure.DI_ code generator.

The composition definition is in the file [Composition.cs](/samples/WinFormsApp/Composition.cs). Remember to define all the necessary roots of the composition, for example, this could be a main form such as _FormMain_:

```csharp
using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    void Setup() => DI.Setup()
        // Provides the composition root for main form
        .Root<Owned<FormMain>>(nameof(Root))

        // Forms
        .Bind().As(Singleton).To<FormMain>()
        
        // View Models
        .Bind().To<ClockViewModel>()

        // Models
        .Bind().To<Log<TT>>()
        .Bind().To(_ => TimeSpan.FromSeconds(1))
        .Bind().To<Clock.Models.Timer>()
        .Bind().As(PerBlock).To<SystemClock>()
    
        // Infrastructure
        .Bind().As(Singleton).To<Dispatcher>();
}
```

A single instance of the _Composition_ class is defined in the _Main_ method of the [Program.cs](/samples/WinFormsApp/Program.cs) file:

```c#
public static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var composition = new Composition();
        using var root = composition.Root;
        Application.Run(root.Value);
    }
}
```

The [project file](/samples/WinFormsApp/WinFormsApp.csproj) looks like this:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.50">
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
