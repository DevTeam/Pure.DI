#### WPF application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WpfAppNetCore)

This example shows how to build a WPF application with Pure.DI, exposing generated view-model roots through XAML resources while keeping object graph validation at compile time.

> [!TIP]
> `Hint.Resolve` is disabled because XAML uses named composition roots (`App`, `Clock`) directly. This keeps the generated API small and avoids service-locator-style access.

The definition of the composition is in [Composition.cs](/samples/WpfAppNetCore/Composition.cs). This class sets up how the object graphs will be created for the application. Do not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```c#
using Pure.DI;
using static Pure.DI.Lifetime;

namespace WpfAppNetCore;

partial class Composition
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")

        .Root<IAppViewModel>(nameof(App))
        .Root<IClockViewModel>(nameof(Clock))

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<DebugLog<TT>>()
        .Bind().To<WpfDispatcher>();
}
```

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/WpfAppNetCore/App.xaml) for later use within the _XAML_ markup everywhere:

```xaml
<Application x:Class="WpfAppNetCore.App" x:ClassModifier="internal"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:app="clr-namespace:WpfAppNetCore"
             StartupUri="/MainWindow.xaml"
             Exit="OnExit">

    <Application.Resources>
        <!--Creates a shared resource of type Composition and with key "Composition",
        which will be further used as a data context in the views.-->
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

creates a shared resource of type `Composition` and with key _"Composition"_, which will be further used as a data context in the views.

Dispose the shared composition from the WPF `Exit` event when the application closes, especially if singleton services implement `IDisposable`.

You can now use bindings to model views without even editing the views `.cs` code files. All previously defined composition roots are now accessible from [markup](/samples/WpfAppNetCore/Views/MainWindow.xaml) without any effort, such as _ClockViewModel_:

```xaml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:app="clr-namespace:WpfAppNetCore"
        mc:Ignorable="d"
        DataContext="{StaticResource Composition}"
        d:DataContext="{d:DesignInstance app:DesignTimeComposition}"
        Title="{Binding App.Title}">
    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding Clock}">
        <TextBlock Text="{Binding Date}" FontSize="64" HorizontalAlignment="Center"/>
        <TextBlock Text="{Binding Time}" FontSize="128" HorizontalAlignment="Center"/>        
    </StackPanel>
</Window>
```

To use bindings in views:

- You can set a shared resource as a data context

  `DataContext="{StaticResource Composition}"`

  and `d:DataContext="{d:DesignInstance app:DesignTimeComposition}"` for the design time

- Use the bindings as usual:

  `Title="{Binding App.Title}"`

The [project file](/samples/WpfAppNetCore/WpfAppNetCore.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
   ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.5.0">
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
