#### Avalonia application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/AvaloniaApp)

This example demonstrates the creation of a [Avalonia](https://avaloniaui.net/) application in the pure DI paradigm using the Pure.DI code generator.

> [!NOTE]
> [Another example](samples/SingleRootAvaloniaApp) with Avalonia shows how to create an application with a single composition root.

The definition of the composition is in [Composition.cs](/samples/AvaloniaApp/Composition.cs). This class setups how the composition of objects will be created for the application. You must not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```csharp
using Pure.DI;
using static Pure.DI.Lifetime;
using static Pure.DI.RootKinds;

namespace AvaloniaApp;

partial class Composition
{
    private void Setup() => DI.Setup()
        .Root<IAppViewModel>(nameof(App), kind: Virtual)
        .Root<IClockViewModel>(nameof(Clock), kind: Virtual)

        .OrdinalAttribute<InitializableAttribute>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<AvaloniaDispatcher>();
}
```

Advantages over classical DI container libraries:
- No performance impact or side effects when creating composition of objects.
- All logic for analyzing the graph of objects, constructors and methods takes place at compile time. Pure.DI notifies the developer at compile time of missing or cyclic dependencies, cases when some dependencies are not suitable for injection, etc.
- Does not add dependencies to any additional assembly.
- Since the generated code uses primitive language constructs to create object compositions and does not use any libraries, you can easily debug the object composition code as regular code in your application.

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/AvaloniaApp/App.axaml) for later use within the _xaml_ markup everywhere:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="AvaloniaApp.App"
             xmlns:app="using:AvaloniaApp"
             RequestedThemeVariant="Default">

  <!-- "Default" ThemeVariant follows system theme variant.
  "Dark" or "Light" are other available options. -->
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>

  <Application.Resources>
    <!--Creates a shared resource of type Composition and with key "Composition",
    which will be further used as a data context in the views.-->
    <app:Composition x:Key="Composition"  />
  </Application.Resources>

</Application>
```

This markup fragment

```xml
<Application.Resources>
    <app:Composition x:Key="Composition" />
</Application.Resources>
```

creates a shared resource of type `Composition` with key _"Composition"_, which will be further used as a data context in the views.

The associated application [App.axaml.cs](/samples/AvaloniaApp/App.axaml.cs) class is looking like:

```c#
public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (Resources[nameof(Composition)] is Composition composition)
        {
            // Assigns the main window/view
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    desktop.MainWindow = new MainWindow();
                    break;

                case ISingleViewApplicationLifetime singleView:
                    singleView.MainView = new MainWindow();
                    break;
            }

            // Handles disposables
            if (ApplicationLifetime is IControlledApplicationLifetime controlledLifetime)
            {
                controlledLifetime.Exit += (_, _) => composition.Dispose();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

Advantages over classical DI container libraries:
- No explicit initialisation of data contexts is required. Data contexts are configured directly in `.axaml` files according to the MVVM approach.
- The code is simpler, more compact, and requires less maintenance effort.
- The main window is created in a pure DI paradigm, and it can be easily supplied with all necessary dependencies via DI as regular types.

You can now use bindings and use the code-behind file-less approach. All previously defined composition roots are now available from [markup](/samples/AvaloniaApp/Views/MainWindow.xaml) without any effort, e.g. _Clock_:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:app="clr-namespace:AvaloniaApp"
        mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
        x:Class="AvaloniaApp.MainWindow"
        DataContext="{StaticResource Composition}"
        x:DataType="app:Composition"
        Design.DataContext="{d:DesignInstance app:DesignTimeComposition}"
        Title="{Binding App.Title}"
        Icon="/Assets/avalonia-logo.ico"
        FontFamily="Consolas"
        FontWeight="Bold">

  <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding Clock}">
    <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center" />
    <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center" />
  </StackPanel>

</Window>
```

To use bindings in views:

- You can set a shared resource as a data context

  `DataContext="{StaticResource Composition}"`

  and `Design.DataContext="{d:DesignInstance app:DesignTimeComposition}"` for the design time

- Specify the data type in the context:

  `xmlns:app="clr-namespace:AvaloniaApp"`

  `x:DataType="app:Composition"`

- Use the bindings as usual:

```xml
  <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding Clock}">
    <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center" />
    <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center" />
  </StackPanel>
```

Advantages over classical DI container libraries:
- The code-behind `.cs` files for views are free of any logic.
- This approach works just as well during design time.
- You can easily use different view models in a single view.
- Bindings depend on properties through abstractions, which additionally ensures weak coupling of types in application. This is in line with the basic principles of DI.

The [project file](/samples/AvaloniaApp/AvaloniaApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.71">
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
