#### Func with arguments

Sometimes an instance can only be created with values known at runtime, such as an id or a name. Injecting a `Func<..., T>` with arguments gives you a factory: values passed at call time are matched by type to the constructor parameters (here `int id` and `string name` of `Person`), while the remaining dependencies, like `IClock`, are resolved from the composition as usual.


```c#
using Shouldly;
using Pure.DI;
using System.Collections.Immutable;

DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Clock>()
    .Bind().To<Person>()
    .Bind().To<Team>()

    // Composition root
    .Root<ITeam>("Team");

var composition = new Composition();
var team = composition.Team;

team.Members.Length.ShouldBe(3);

team.Members[0].Id.ShouldBe(10);
team.Members[0].Name.ShouldBe("Nik");

team.Members[1].Id.ShouldBe(20);
team.Members[1].Name.ShouldBe("Mike");

team.Members[2].Id.ShouldBe(30);
team.Members[2].Name.ShouldBe("Jake");

interface IClock
{
    DateTimeOffset Now { get; }
}

class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

interface IPerson
{
    int Id { get; }

    string Name { get; }
}

class Person(string name, IClock clock, int id)
    : IPerson
{
    public int Id => id;

    public string Name => name;
}

interface ITeam
{
    ImmutableArray<IPerson> Members { get; }
}

class Team(Func<int, string, IPerson> personFactory) : ITeam
{
    public ImmutableArray<IPerson> Members { get; } =
    [
        personFactory(10, "Nik"),
        personFactory(20, "Mike"),
        personFactory(30, "Jake")
    ];
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to the NuGet packages
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

>[!NOTE]
>Func with arguments provides flexibility for scenarios where you need to pass runtime parameters during instance creation.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
#if NET9_0_OR_GREATER
  private readonly Lock _lock = new Lock();
#else
  private readonly Object _lock = new Object();
#endif

  private Clock? _singletonCompositionInOtherProject;

  public ITeam Team
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Func<int, string, IPerson> perBlockFuncInt32StringIPerson;
      // Creates a factory with runtime arguments
      Func<int, string, IPerson> localFactory = new Func<int, string, IPerson>((int localArg11, string localArg2) =>
      {
        // Creates the result
        int overriddenInt32 = localArg11;
        string overriddenString = localArg2;
        if (_singletonCompositionInOtherProject is null)
          lock (_lock)
            if (_singletonCompositionInOtherProject is null)
            {
              _singletonCompositionInOtherProject = new Clock();
            }

        return new Person(overriddenString, _singletonCompositionInOtherProject, overriddenInt32);
      });
      perBlockFuncInt32StringIPerson = localFactory;
      return new Team(perBlockFuncInt32StringIPerson);
    }
  }
}
```

</details>

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	Person --|> IPerson
	Team --|> ITeam
	Composition ..> Team : ITeam Team
	Person o-- "Singleton" Clock : IClock
	Person *-- Int32 : Int32
	Person *-- String : String
	Team o-- "PerBlock" FuncᐸInt32ˏStringˏIPersonᐳ : FuncᐸInt32ˏStringˏIPersonᐳ
	FuncᐸInt32ˏStringˏIPersonᐳ *-- Person : IPerson
	namespace Pure.DI.UsageTests.BCL.FuncWithArgumentsScenario {
		class Clock {
			<<class>>
		}
		class Composition {
		<<partial>>
		+ITeam Team
		}
		class IPerson {
			<<interface>>
		}
		class ITeam {
			<<interface>>
		}
		class Person {
				<<class>>
			+Person(String name, IClock clock, Int32 id)
		}
		class Team {
				<<class>>
			+Team(FuncᐸInt32ˏStringˏIPersonᐳ personFactory)
		}
	}
	namespace System {
		class FuncᐸInt32ˏStringˏIPersonᐳ {
				<<delegate>>
		}
		class Int32 {
			<<struct>>
		}
		class String {
			<<class>>
		}
	}
```

See also:

- [Func](func.md)
- [Injections on demand with arguments](injections-on-demand-with-arguments.md)
- [Overrides](overrides.md)

