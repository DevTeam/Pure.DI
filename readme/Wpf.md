#### WPF application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WpfAppNetCore)

This example demonstrates the creation of a WPF application in the pure DI paradigm using the Pure.DI code generator.

The definition of the composition is in [Composition.cs](/samples/WpfAppNetCore/Composition.cs). This class setups how the composition of objects will be created for the application. You must not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```csharp
using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition
{
    void Setup() => DI.Setup()
        // Provides the composition root for clock view model
        .Root<IClockViewModel>(nameof(ClockViewModel))
        
        // View Models
        .Bind().As(Singleton).To<ClockViewModel>()

        // Models
        .Bind().To<Log<TT>>()
        .Bind().As(Singleton).To<Timer>()
        .Bind().As(PerBlock).To<SystemClock>()
    
        // Infrastructure
        .Bind().To<Dispatcher>();
}
```

Advantages over classical DI container libraries:
- No performance impact or side effects when creating composition of objects.
- All logic for analyzing the graph of objects, constructors and methods takes place at compile time. Pure.DI notifies the developer at compile time of missing or cyclic dependencies, cases when some dependencies are not suitable for injection, etc.
- Does not add dependencies to any additional assembly.
- Since the generated code uses primitive language constructs to create object compositions and does not use any libraries, you can easily debug the object composition code as regular code in your application.

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/WpfAppNetCore/App.xaml) for later use within the _xaml_ markup everywhere:

```xaml
<Application x:Class="WpfAppNetCore.App" x:ClassModifier="internal"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:app="clr-namespace:WpfAppNetCore"
             StartupUri="/Views/MainWindow.xaml"
             Exit="OnExit">

    <!--Creates a shared resource of type `Composition` and with key _‘Composition’_,
    which will be further used as a data context in the views.-->
    <Application.Resources>
        <app:Composition x:Key="Composition" />
    </Application.Resources>

</Application>
```

This markup fragment

```xml
<Application.Resources>
    <app:Composition x:Key="Composition" />
</Application.Resources>
```

creates a shared resource of type `Composition` and with key _‘Composition’_, which will be further used as a data context in the views.

Advantages over classical DI container libraries:
- No explicit initialisation of data contexts is required. Data contexts are configured directly in `.axaml` files according to the MVVM approach.
- The code is simpler, more compact, and requires less maintenance effort.
- The main window is created in a pure DI paradigm, and it can be easily supplied with all necessary dependencies via DI as regular types.

You can now use bindings to model views without even editing the views `.cs` code files. All previously defined composition roots are now accessible from [markup](/samples/WpfAppNetCore/Views/MainWindow.xaml) without any effort, such as _ClockViewModel_:

```xaml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        DataContext="{StaticResource Composition}"
        Title="{Binding ClockViewModel.Time}">
    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding ClockViewModel}">
        <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center"/>
        <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center"/>        
    </StackPanel>
</Window>
```

To use bindings in views:

- You can set a shared resource as a data context

  `DataContext="{StaticResource Composition}"`

- Use the bindings as usual:

  `Title="{Binding ClockViewModel.Time}"`

Advantages over classical DI container libraries:
- The code-behind `.cs` files for views are free of any logic.
- This approach works just as well during design time.
- You can easily use different view models in a single view.
- Bindings depend on properties through abstractions, which additionally ensures weak coupling of types in application. This is in line with the basic principles of DI.

The [project file](/samples/WpfAppNetCore/WpfAppNetCore.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <UseWPF>true</UseWPF>
        <TargetFramework>net9.0-windows</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.66">
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
