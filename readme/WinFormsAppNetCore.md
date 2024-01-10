#### WinForms application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WinFormsAppNetCore)

This example demonstrates the creation of a WinForms application in the pure DI paradigm using the _Pure.DI_ code generator.

The composition definition is in the file [Composition.cs](/samples/WinFormsAppNetCore/Composition.cs). Remember to define all the necessary roots of the composition, for example, this could be a main form such as _FormMain_:

```csharp
internal partial class Composition
{
    private static void Setup() => DI.Setup(nameof(Composition))
        .Root<FormMain>("FormMain")

        // Forms
        .Bind<FormMain>().As(Singleton).To<FormMain>()
        
        // View Models
        .Bind<IClockViewModel>().To<ClockViewModel>()

        // Models
        .Bind<ILog<TT>>().To<Log<TT>>()
        .Bind<TimeSpan>().To(_ => TimeSpan.FromSeconds(1))
        .Bind<ITimer>().As(Singleton).To<Clock.Models.Timer>()
        .Bind<IClock>().To<SystemClock>()
    
        // Infrastructure
        .Bind<IDispatcher>().To<Dispatcher>();
}
```

A single instance of the _Composition_ class is defined in the _Main_ method of the [Program.cs](/samples/WinFormsAppNetCore/Program.cs) file:

```c#
public static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        var composition = new Composition();
        Application.Run(composition.FormMain);
    }
}
```

The [project file](/samples/WinFormsAppNetCore/WinFormsAppNetCore.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <UseWPF>true</UseWPF>
        <TargetFramework>net8.0-windows</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.0.38">
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
