#### WPF application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/WpfAppNetCore)

This example demonstrates the creation of a WPF application in the pure DI paradigm using the _Pure.DI_ code generator.

The definition of the composition is in [Composition.cs](/samples/WpfAppNetCore/Composition.cs). You must not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```csharp
internal partial class Composition
{
    private static void Setup() => DI.Setup(nameof(Composition))
        // View Models
        .Bind<IClockViewModel>().To<ClockViewModel>().Root<IClockViewModel>("ClockViewModel")

        // Models
        .Bind<ILog<TT>>().To<Log<TT>>()
        .Bind<TimeSpan>().To(_ => TimeSpan.FromSeconds(1))
        .Bind<ITimer>().As(Lifetime.Singleton).To<Clock.Models.Timer>()
        .Bind<IClock>().To<SystemClock>()
    
        // Infrastructure
        .Bind<IDispatcher>().To<Dispatcher>();
}
```

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/WpfAppNetCore/App.xaml) for later use within the _xaml_ markup everywhere:

```xaml
<Application x:Class="WpfAppNetCore.App" x:ClassModifier="internal"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:wpfAppNetCore="clr-namespace:WpfAppNetCore"
             StartupUri="/Views/MainWindow.xaml"
             Exit="OnExit">
    <Application.Resources>
        <wpfAppNetCore:Composition x:Key="Composition"/>
    </Application.Resources>
</Application>
```

All previously defined composition roots are now accessible from [markup](/samples/WpfAppNetCore/Views/MainWindow.xaml) without any effort, such as _ClockViewModel_:

```xaml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        DataContext="{StaticResource Composition}">
    <Grid>
        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" DataContext="{Binding ClockViewModel}">
            <TextBlock Text="{Binding Date}" FontSize="64" />
            <TextBlock Text="{Binding Time}" FontSize="64" />
            <StackPanel.Effect>
                <DropShadowEffect Color="Black" Direction="20" ShadowDepth="5" Opacity="0.5" />
            </StackPanel.Effect>
        </StackPanel>
    </Grid>
</Window>
```

The [project file](/samples/WpfAppNetCore/WpfAppNetCore.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <UseWPF>true</UseWPF>
        <TargetFramework>net7.0-windows</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.0.11">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        ...
    </ItemGroup>
</Project>
```