#### Console Native AOT application

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/ShroedingersCatNativeAOT)

This example shows how the simple console composition can be published as a [native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) application. Pure.DI generates plain C# object creation code, so the dependency graph remains friendly to trimming and ahead-of-time compilation.

> [!TIP]
> Native AOT works best when construction is explicit and reflection-light. Prefer generated roots and bindings over runtime service-location patterns in AOT samples.

The [project file](/samples/ShroedingersCatNativeAOT/ShroedingersCatNativeAOT.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="$(version)">
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
