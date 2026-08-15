#### Uno Platform application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/UnoApp)

This example shows how to use Pure.DI as a compile-time dependency injection solution in an [Uno Platform](https://platform.uno/) application. The sample is a single C# and XAML project configured for the Uno Skia Desktop target, while Pure.DI generates the view-model and infrastructure object graphs at compile time.

> [!TIP]
> The sample uses the minimal Uno Platform Blank preset and explicit Pure.DI roots. It does not add a runtime DI container, and `Hint.Resolve` is disabled.

##### When to use Pure.DI with Uno Platform

Pure.DI is a good fit when you want the object graph to be validated and generated at compile time, prefer explicit composition roots, and do not need to register application services in a runtime `IServiceCollection`. Generated roots are ordinary properties, so they can be exposed to Uno XAML through an application resource without a service locator.

Uno Platform also offers `Uno.Extensions.DependencyInjection`, which follows the `Microsoft.Extensions.Hosting` and `IServiceProvider` model. Prefer that standard Uno hosting path when the application relies on Uno Extensions, navigation, or other components that expect services to be registered and resolved at runtime. The approach shown here is an alternative for a Blank application; it is not a replacement for every Uno hosting scenario.

##### Verified scope

The sample deliberately keeps its target surface small. Repository verification covers compilation of Pure.DI source generation, Uno XAML resources, design-time roots, and lifecycle wiring in a `net10.0-desktop` project. Runtime behavior must still be validated on every operating system the application ships on.

| Target                                         | Sample status                                                                                   |
|------------------------------------------------|-------------------------------------------------------------------------------------------------|
| Skia Desktop, `net10.0-desktop`                | Included and build-verified                                                                     |
| Windows, macOS, and Linux desktop execution    | Supported by the configured Uno Skia Desktop target; validate on each operating system you ship |
| Windows App SDK, WebAssembly, Android, and iOS | Not demonstrated by this sample                                                                 |

This table describes the coverage of this sample, not a limitation imposed by the Pure.DI generator. Add the required Uno target frameworks and perform platform-specific build and runtime checks before claiming support for additional targets in an application.

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

##### Lifecycle and disposal

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
        <PackageReference Include="Pure.DI" Version="2.5.3">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>
</Project>
```

The repository sample references the local Pure.DI projects so changes to the generator can be tested directly. Applications consuming a released version should use the `PackageReference` shown above.

##### Build and run the sample

From the repository root, restore, build, and run the verified Skia Desktop target:

```shell
dotnet restore samples/UnoApp/UnoApp.csproj
dotnet build samples/UnoApp/UnoApp.csproj -f net10.0-desktop
dotnet run --project samples/UnoApp/UnoApp.csproj -f net10.0-desktop
```

Run the application on every desktop operating system you intend to support. A successful build verifies source generation and compilation; launching it also verifies XAML resource creation, bindings, dispatcher access, and disposal for that runtime.

|              |                                                                                            |                                    |
|--------------|--------------------------------------------------------------------------------------------|:-----------------------------------|
| Pure.DI      | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI) | DI source code generator           |
| Uno Platform | [Documentation](https://platform.uno/docs/)                                                | Cross-platform WinUI-compatible UI |
