#### Entity Framework

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](/samples/EF)

This example shows how to combine Pure.DI with Entity Framework services supplied by Microsoft dependency injection. Pure.DI builds the application graph, while the external service provider supplies the `DbContext` and EF infrastructure.

> [!TIP]
> `PersonsDbContext` is intentionally not bound in Pure.DI. It is requested from the external `ServiceProvider`, while Pure.DI owns the application root and factories such as `Func<Person>`.

The composition setup file is [Composition.cs](/samples/EF/Composition.cs):

```c#
using System.Diagnostics;
using Pure.DI;
using Pure.DI.MS;

namespace EF;

partial class Composition : ServiceProviderFactory<Composition>
{
    [System.Diagnostics.Conditional("DI")]
    private void Setup() => DI.Setup()
        .Root<Program>(nameof(Root))

        .Bind().As(Lifetime.PerResolve).To<PersonService>()
        .Bind().As(Lifetime.Singleton).To<ContactService>();
}
```

The composition class inherits from `ServiceProviderFactory<T>`, where `T` is the composition class itself. When Pure.DI cannot resolve framework services such as `DbContext`, `ServiceProviderFactory<T>` delegates those requests to the configured Microsoft dependency injection provider.

The console application entry point is in the [Program.cs](/samples/EF/Program.cs) file:

```c#
using EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var composition = new Composition
{
    ServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .AddDbContext<PersonsDbContext>(options => options.UseInMemoryDatabase("Database of persons"))
        .BuildServiceProvider()
};

var root = composition.Root;
await root.RunAsync();

partial class Program(
    PersonsDbContext db,
    Func<Person> newPerson,
    Func<Contact> newContact)
{
    private async Task RunAsync()
    {
        var nik = newPerson() with
        {
            Name = "Nik",
            Contacts =
            [
                newContact() with { PhoneNumber = "+123456789" }
            ]
        };

        db.Persons.Add(nik);

        var john = newPerson() with
        {
            Name = "John",
            Contacts =
            [
                newContact() with { PhoneNumber = "+777333444" },
                newContact() with { PhoneNumber = "+999888666" }
            ]
        };

        db.Persons.Add(john);

        await db.SaveChangesAsync();
        await db.Persons.ForEachAsync(Console.WriteLine);
    }
}
```

The external service provider should be configured before resolving `composition.Root`. If a required EF service is missing, Pure.DI will fail when the root is created instead of hiding the missing registration.

The [project file](/samples/EF/EF.csproj) looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="Pure.DI" Version="$(version)">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Pure.DI.MS" Version="$(ms.version)" />
    </ItemGroup>

</Project>
```

It contains additional references to NuGet packages:

|            |                                                                                                  |                                               |
|------------|--------------------------------------------------------------------------------------------------|:----------------------------------------------|
| Pure.DI    | [![NuGet](https://img.shields.io/nuget/v/Pure.DI)](https://www.nuget.org/packages/Pure.DI)       | DI source code generator                      |
| Pure.DI.MS | [![NuGet](https://img.shields.io/nuget/v/Pure.DI.MS)](https://www.nuget.org/packages/Pure.DI.MS) | Add-ons for Pure.DI to work with Microsoft DI |
