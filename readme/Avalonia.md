#### Avalonia application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/AvaloniaApp)

This example demonstrates the creation of a [Avalonia](https://avaloniaui.net/) application in the pure DI paradigm using the _Pure.DI_ code generator.

The definition of the composition is in [Composition.cs](/samples/AvaloniaApp/Composition.cs). You must not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```csharp
internal partial class Composition
{
    void Setup() => DI.Setup(nameof(Composition))
        // Provides the composition root for main window
        .Root<MainWindow>("MainWindow")
        // Provides the composition root for Clock view model
        .Root<IClockViewModel>("ClockViewModel")
        
        // View Models
        .Bind().As(Singleton).To<ClockViewModel>()

        // Models
        .Bind().To<Log<TT>>()
        .Bind().To(_ => TimeSpan.FromSeconds(1))
        .Bind().As(Singleton).To<Clock.Models.Timer>()
        .Bind().As(PerBlock).To<SystemClock>()
    
        // Infrastructure
        .Bind().To<Dispatcher>();
}
```

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/AvaloniaApp/App.axaml) for later use within the _xaml_ markup everywhere:

```xaml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="AvaloniaApp.App"
             xmlns:local="using:AvaloniaApp"
             RequestedThemeVariant="Default">
    <Application.Resources>
        <local:Composition x:Key="Composition" />
    </Application.Resources>
    <!-- "Default" ThemeVariant follows system theme variant. "Dark" or "Light" are other available options. -->
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

The associated application [App.axaml.cs](/samples/AvaloniaApp/App.axaml.cs) class is looking like:

```c#
public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && Resources["Composition"] is Composition composition)
        {
            // Assignment of the main window
            desktop.MainWindow = composition.MainWindow;
            // Handles disposables
            desktop.Exit += (_, _) => composition.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

All previously defined composition roots are now accessible from [markup](/samples/AvaloniaApp/Views/MainWindow.xaml) without any effort, such as _ClockViewModel_:

```xaml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:avaloniaApp="clr-namespace:AvaloniaApp"
        mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
        x:Class="AvaloniaApp.Views.MainWindow"
        x:DataType="avaloniaApp:Composition"
        DataContext="{StaticResource Composition}"
        Title="{Binding ClockViewModel.Time}"
        Icon="/Assets/avalonia-logo.ico">

    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding ClockViewModel}">
        <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center" />
        <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center" />
    </StackPanel>

</Window>
```

The [project file](/samples/AvaloniaApp/AvaloniaApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <OutputType>WinExe</OutputType>
        <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
        <ApplicationManifest>app.manifest</ApplicationManifest>
        <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.8">
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
