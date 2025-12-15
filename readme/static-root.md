#### Static root


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.PerResolve).To<FileSystem>()
    .Bind().To<Configuration>()
    .Root<IConfiguration>("GlobalConfiguration", kind: RootKinds.Static);

var configuration = Composition.GlobalConfiguration;
configuration.ShouldBeOfType<Configuration>();

interface IFileSystem;

class FileSystem : IFileSystem;

interface IConfiguration;

class Configuration(IFileSystem fileSystem) : IConfiguration;
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later is installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

The following partial class will be generated:

```c#
partial class Composition
{
  [OrdinalAttribute(256)]
  public Composition()
  {
  }

  internal Composition(Composition parentScope)
  {
  }

  public static IConfiguration GlobalConfiguration
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new Configuration(new FileSystem());
    }
  }
}
```

Class diagram:

```mermaid
---
 config:
  maxTextSize: 2147483647
  maxEdges: 2147483647
  class:
   hideEmptyMembersBox: true
---
classDiagram
	FileSystem --|> IFileSystem
	Configuration --|> IConfiguration
	Composition ..> Configuration : IConfiguration GlobalConfiguration
	Configuration o-- "PerResolve" FileSystem : IFileSystem
	namespace Pure.DI.UsageTests.Basics.StaticRootScenario {
		class Composition {
		<<partial>>
		+IConfiguration GlobalConfiguration
		}
		class Configuration {
				<<class>>
			+Configuration(IFileSystem fileSystem)
		}
		class FileSystem {
				<<class>>
			+FileSystem()
		}
		class IConfiguration {
			<<interface>>
		}
		class IFileSystem {
			<<interface>>
		}
	}
```

