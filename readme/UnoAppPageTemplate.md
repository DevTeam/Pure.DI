#### Uno Platform application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/UnoApp)

This example shows how to build a cross-platform [Uno Platform](https://platform.uno/) application with Pure.DI. One C# and XAML project targets Windows, macOS, and Linux through the Skia Desktop runtime, while Pure.DI generates the view-model and infrastructure object graphs at compile time.

> [!TIP]
> The sample uses the minimal Uno Platform Blank preset and explicit Pure.DI roots. It does not add a runtime DI container, and `Hint.Resolve` is disabled.

The composition is defined in [Composition.cs](/samples/UnoApp/Composition.cs). Its virtual roots are available to XAML and can be replaced by the design-time composition:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;
using static Pure.DI.RootKinds;

namespace UnoApp;

public partial class Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<IAppViewModel>(nameof(App), kind: Virtual)
        .Root<IClockViewModel>(nameof(Clock), kind: Virtual)

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<UnoDispatcher>();
}
```

[DesignTimeComposition.cs](/samples/UnoApp/DesignTimeComposition.cs) overrides the same roots with predictable data for XAML tooling:

```c#
using Pure.DI;
using static Pure.DI.RootKinds;

namespace UnoApp;

public partial class DesignTimeComposition : Composition
{
    [Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<IAppViewModel>(nameof(App), kind: Override)
        .Root<IClockViewModel>(nameof(Clock), kind: Override)

        .Bind().To<DesignTimeAppViewModel>()
        .Bind().To<DesignTimeClockViewModel>();
}
```

A shared `Composition` is declared in [App.xaml](/samples/UnoApp/App.xaml), just like any other XAML resource:

```xml
<Application x:Class="UnoApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:app="using:UnoApp">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            </ResourceDictionary.MergedDictionaries>

            <app:Composition x:Key="Composition" />
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

The application retrieves that resource when it creates the main window and disposes it when the window closes. This releases disposable singleton and scoped dependencies owned by Pure.DI:

```c#
private Window? MainWindow { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    if (Resources[nameof(Composition)] is not Composition composition)
    {
        throw new InvalidOperationException("The Pure.DI composition resource was not found.");
    }

    var window = MainWindow = new Window
    {
        Content = new MainPage()
    };

    var appViewModel = composition.App;
    window.Title = appViewModel.Title;

    PropertyChangedEventHandler? titleChanged = null;
    if (appViewModel is INotifyPropertyChanged propertyChanged)
    {
        titleChanged = (_, eventArgs) =>
        {
            if (eventArgs.PropertyName is null or nameof(IAppViewModel.Title))
            {
                window.Title = appViewModel.Title;
            }
        };

        propertyChanged.PropertyChanged += titleChanged;
    }

    window.Closed += (_, _) =>
    {
        if (appViewModel is INotifyPropertyChanged propertyChanged && titleChanged is not null)
        {
            propertyChanged.PropertyChanged -= titleChanged;
        }

        composition.Dispose();
    };

    window.Activate();
}
```

[MainPage.xaml](/samples/UnoApp/MainPage.xaml) uses the composition as its data context. The generated `App` and `Clock` properties are ordinary binding paths:

```xml
<Page x:Class="UnoApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns:app="using:UnoApp"
      mc:Ignorable="d"
      DataContext="{StaticResource Composition}"
      d:DataContext="{d:DesignInstance Type=app:DesignTimeComposition, IsDesignTimeCreatable=True}"
      FontFamily="Consolas"
      FontWeight="Bold">
    <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center"
                DataContext="{Binding Clock}">
        <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center" />
        <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center" />
    </StackPanel>
</Page>
```

The [project file](/samples/UnoApp/UnoApp.csproj) uses the current single-project Uno SDK and adds Pure.DI as a source generator:

```xml
<Project Sdk="Uno.Sdk/6.5.36">
    <PropertyGroup>
        <TargetFrameworks>net10.0-desktop</TargetFrameworks>
        <UnoSingleProject>true</UnoSingleProject>
        <UnoFeatures>SkiaRenderer;</UnoFeatures>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="$(version)">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>
</Project>
```

|              |                                                                                          |                                      |
|--------------|------------------------------------------------------------------------------------------|:-------------------------------------|
| Pure.DI      | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator             |
| Uno Platform | [Documentation](https://platform.uno/docs/)                                               | Cross-platform WinUI-compatible UI   |
