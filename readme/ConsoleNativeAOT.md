#### Console native AOT application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCatNativeAOT)

This example is very similar to [simple console application](ConsoleTemplate.md), except that this is [native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) application.

The [project file](/samples/ShroedingersCatNativeAOT/ShroedingersCatNativeAOT.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <PublishAot>true</PublishAot>
        <RuntimeIdentifier>win-x64</RuntimeIdentifier>
        <OutputType>Exe</OutputType>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="2.1.31">
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
