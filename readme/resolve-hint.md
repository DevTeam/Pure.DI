#### Resolve hint

Hints are used to fine-tune code generation. The _Resolve_ hint determines whether to generate _Resolve_ methods. By default, a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce class composition generation time, and no anonymous composition roots will be generated in this case. When the _Resolve_ hint is disabled, only the regular root properties are available, so be sure to define them explicitly with the `Root<T>(...)` method.
In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// Resolve = Off`.


```c#
using Pure.DI;
using static Pure.DI.Hint;

DI.Setup(nameof(Composition))
    .Hint(Resolve, "Off")
    .Bind().To<DatabaseService>()
    // When the "Resolve" hint is disabled, only the regular root properties
    // are available, so be sure to define them explicitly
    // with the "Root<T>(...)" method.
    .Root<IDatabaseService>("DatabaseService")
    .Bind().To<UserService>()
    .Root<IUserService>("UserService");

var composition = new Composition();

// The "Resolve" method is not generated,
// so we can only access the roots through properties:
var userService = composition.UserService;
var databaseService = composition.DatabaseService;

// composition.Resolve<IUserService>(); // Compile error

interface IDatabaseService;

class DatabaseService : IDatabaseService;

interface IUserService;

class UserService(IDatabaseService database) : IUserService;
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
- Add reference to NuGet package
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
```bash
dotnet add package Pure.DI
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

For more hints, see [this](README.md#setup-hints) page.

The following partial class will be generated:

```c#
partial class Composition
{
  public IDatabaseService DatabaseService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new DatabaseService();
    }
  }

  public IUserService UserService
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new UserService(new DatabaseService());
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
	DatabaseService --|> IDatabaseService
	UserService --|> IUserService
	Composition ..> UserService : IUserService UserService
	Composition ..> DatabaseService : IDatabaseService DatabaseService
	UserService *--  DatabaseService : IDatabaseService
	namespace Pure.DI.UsageTests.Hints.ResolveHintScenario {
		class Composition {
		<<partial>>
		+IDatabaseService DatabaseService
		+IUserService UserService
		}
		class DatabaseService {
				<<class>>
			+DatabaseService()
		}
		class IDatabaseService {
			<<interface>>
		}
		class IUserService {
			<<interface>>
		}
		class UserService {
				<<class>>
			+UserService(IDatabaseService database)
		}
	}
```

