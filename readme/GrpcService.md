#### gRPC service

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/GrpcService)

This example demonstrates the creation of a gRPC service in the pure DI paradigm using the Pure.DI code generator.

The composition setup file is [Composition.cs](/samples/GrpcService/Composition.cs):

```c#
using Pure.DI;
using Pure.DI.MS;
using static Pure.DI.Lifetime;

namespace GrpcService;

partial class Composition : ServiceProviderFactory<Composition>
{
    private void Setup() => DI.Setup()
        .Root<ClockService>()

        .Bind().As(Singleton).To<ClockViewModel>()
        .Bind().To<ClockModel>()
        .Bind().As(Singleton).To<Ticks>()

        // Infrastructure
        .Bind().To<MicrosoftLoggerAdapter<TT>>()
        .Bind().To<CurrentThreadDispatcher>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself.

The web application entry point is in the [Program.cs](/samples/GrpcService/Program.cs) file:

```c#
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

using var composition = new Composition();

// Uses Composition as an alternative IServiceProviderFactory
builder.Host.UseServiceProviderFactory(composition);
...
```

The [project file](/samples/GrpcService/GrpcService.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.3.5">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="2.3.5" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |
