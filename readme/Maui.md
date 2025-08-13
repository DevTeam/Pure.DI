#### MAUI application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/MAUIApp)

This example demonstrates the creation of a [MAUI application](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui) in the pure DI paradigm using the Pure.DI code generator.

The definition of the composition is in [Composition.cs](/samples/MAUIApp/Composition.cs). You must not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```csharp
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace MAUIApp;

partial class Composition: ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .DependsOn(Base)

        .Root<IAppViewModel>(nameof(App))
        .Root<IClockViewModel>(nameof(Clock))

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<MauiDispatcher>();
}
```

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself. It depends on the `Base` setup.

The web application entry point is in the [MauiProgram.cs](/samples/MAUIApp/MauiProgram.cs) file:

```c#
namespace MAUIApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var composition = new Composition();
        
        // Uses Composition as an alternative IServiceProviderFactory
        builder.ConfigureContainer(composition);
        
        builder
            .UseMauiApp(_ => new App
            {
                // Overrides the resource with an initialized Composition instance
                Resources = { ["Composition"] = composition }
            })
            .ConfigureLifecycleEvents(events =>
            {
                // Handles disposables
#if WINDOWS
                events.AddWindows(windows => windows
                    .OnClosed((_, _) => composition.Dispose()));
#endif
#if ANDROID
                events.AddAndroid(android => android
                    .OnStop(_ => composition.Dispose()));
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

A single instance of the _Composition_ class is defined as a static resource in [App.xaml](/samples/MAUIApp/App.xaml) for later use within the _xaml_ markup everywhere:

```xaml
<?xml version="1.0" encoding="UTF-8"?>

<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:MAUIApp"
             x:Class="MAUIApp.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <local:Composition x:Key="Composition" />
                </ResourceDictionary>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

All previously defined composition roots are now accessible from [markup](/samples/MAUIApp/MainWindow.xaml) without any effort:

```xaml
<?xml version="1.0" encoding="utf-8"?>

<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MAUIApp.MainPage"
             xmlns:local="clr-namespace:MAUIApp"
             xmlns:clock="clr-namespace:Clock;assembly=Clock"
             BindingContext="{Binding Source={StaticResource Composition}, x:DataType=local:Composition, Path=Clock}"
             x:DataType="clock:IClockViewModel">

    <ScrollView>

        <VerticalStackLayout
            Spacing="25"
            Padding="30,0"
            VerticalOptions="Center">

            <Label
                Text="{Binding Date}"
                SemanticProperties.HeadingLevel="Level1"
                FontSize="64"
                HorizontalOptions="Center" />

            <Label
                Text="{Binding Time}"
                SemanticProperties.HeadingLevel="Level2"
                FontSize="128"
                HorizontalOptions="Center" />

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

The [project file](/samples/MAUIApp/MAUIApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.2.3">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.2.3" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |

