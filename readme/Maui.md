#### MAUI application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/MAUIApp)

This example demonstrates the creation of a [MAUI application](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui) in the pure DI paradigm using the Pure.DI code generator.

The definition of the composition is in [Composition.cs](/samples/MAUIApp/Composition.cs). You must not forget to define any necessary composition roots, for example, these can be view models such as _ClockViewModel_:

```csharp
using Pure.DI;
using static Pure.DI.Lifetime;

internal partial class Composition: ServiceProviderFactory<Composition>
{
    // IMPORTANT:
    // Only composition roots (regular or anonymous) can be resolved through the `IServiceProvider` interface.
    // These roots must be registered using `Root(...)` or `RootBind()` calls.
    void Setup() => DI.Setup()
        // Use the DI setup from the base class
        .DependsOn(Base)

        // Roots
        .Root<AppShell>(nameof(AppShell))
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

The composition class inherits from the `ServiceProviderFactory<T>` class, where T is the composition class itself. It depends on the `Base` setup.

The web application entry point is in the [MauiProgram.cs](/samples/MAUIApp/MauiProgram.cs) file:

```c#
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var composition = new Composition();
        
        // Uses Composition as an alternative IServiceProviderFactory
        builder.ConfigureContainer(composition);
        
        builder
            .UseMauiApp(_ => new App(composition))
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

All previously defined composition roots are now accessible from [markup](/samples/MAUIApp/MainWindow.xaml) without any effort, such as _ClockViewModel_:

```xaml
<?xml version="1.0" encoding="utf-8"?>

<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MAUIApp.MainPage"
             BindingContext="{StaticResource Composition}">

    <ScrollView>
        <VerticalStackLayout
            Spacing="25"
            Padding="30,0"
            VerticalOptions="Center"
            BindingContext="{Binding ClockViewModel}">

            <Image
                Source="dotnet_bot.png"
                SemanticProperties.Description="Cute dot net bot waving hi to you!"
                HeightRequest="200"
                HorizontalOptions="Center" />

            <Label
                Text="{Binding Date}"
                SemanticProperties.HeadingLevel="Level1"
                FontSize="32"
                HorizontalOptions="Center" />

            <Label
                Text="{Binding Time}"
                SemanticProperties.HeadingLevel="Level2"
                SemanticProperties.Description="Welcome to dot net Multi platform App U I"
                FontSize="18"
                HorizontalOptions="Center" />
            
        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

The [project file](/samples/MAUIApp/MAUIApp.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFrameworks>net8.0-android;net8.0-ios;net8.0-maccatalyst</TargetFrameworks>
        <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net8.0-windows10.0.19041.0</TargetFrameworks>
        <!-- Uncomment to also build the tizen app. You will need to install tizen by following this: https://github.com/Samsung/Tizen.NET -->
        <!-- <TargetFrameworks>$(TargetFrameworks);net8.0-tizen</TargetFrameworks> -->

        <!-- Note for MacCatalyst:
        The default runtime is maccatalyst-x64, except in Release config, in which case the default is maccatalyst-x64;maccatalyst-arm64.
        When specifying both architectures, use the plural <RuntimeIdentifiers> instead of the singular <RuntimeIdentifer>.
        The Mac App Store will NOT accept apps with ONLY maccatalyst-arm64 indicated;
        either BOTH runtimes must be indicated or ONLY macatalyst-x64. -->
        <!-- For example: <RuntimeIdentifiers>maccatalyst-x64;maccatalyst-arm64</RuntimeIdentifiers> -->

        <OutputType>Exe</OutputType>
        <RootNamespace>MAUIApp</RootNamespace>
        <UseMaui>true</UseMaui>
        <SingleProject>true</SingleProject>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

        <!-- Display name -->
        <ApplicationTitle>MAUIApp</ApplicationTitle>

        <!-- App Identifier -->
        <ApplicationId>com.companyname.mauiapp</ApplicationId>

        <!-- Versions -->
        <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
        <ApplicationVersion>1</ApplicationVersion>

        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">11.0</SupportedOSPlatformVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'">13.1</SupportedOSPlatformVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">21.0</SupportedOSPlatformVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
        <TargetPlatformMinVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</TargetPlatformMinVersion>
        <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'tizen'">6.5</SupportedOSPlatformVersion>
    </PropertyGroup>

    <ItemGroup>
        <!-- App Icon -->
        <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#512BD4"/>

        <!-- Splash Screen -->
        <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#512BD4" BaseSize="128,128"/>

        <!-- Images -->
        <MauiImage Include="Resources\Images\*"/>
        <MauiImage Update="Resources\Images\dotnet_bot.svg" BaseSize="168,208"/>

        <!-- Custom Fonts -->
        <MauiFont Include="Resources\Fonts\*"/>

        <!-- Raw Assets (also remove the "Resources\Raw" prefix) -->
        <MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)"/>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)"/>
        <PackageReference Include="Microsoft.Maui.Controls.Compatibility" Version="$(MauiVersion)"/>
        <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.0"/>
        <PackageReference Include="Pure.DI" Version="2.1.61">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.1.61" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                              |
|------------|--------------------------------------------------------------------------------------------------|:---------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                     |

