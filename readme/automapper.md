#### AutoMapper

AutoMapper creates target objects itself, so mapped instances normally bypass DI even when they need dependencies. Here a configured `IMapper` is bound as a singleton, and a generic `Func<TT1, TT2>` binding wraps `mapper.Map` so that consumers like `StudentService` simply inject a mapping function for the types they need. After mapping, `ctx.BuildUp(target)` injects the members marked with `[Inject]` — such as `Person.Formatter` — into the freshly mapped object.


```c#
using Shouldly;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI.Abstractions;
using Pure.DI;
using Pure.DI.Abstractions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using static Pure.DI.Lifetime;
using static Pure.DI.Tag;

var logMessages = new List<string>();
using var composition = new Composition(logMessages);
var root = composition.Root;

root.Run();
logMessages.ShouldContain("John Smith");

class Person
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? BirthDate { get; set; }

    [Inject]
    public IPersonFormatter? Formatter { get; set; }
}

class Student
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? BirthDate { get; set; }

    public DateTime? AdmissionDate { get; set; }
}

interface IPersonFormatter
{
    string Format(Person person);
}

class PersonFormatter : IPersonFormatter
{
    public string Format(Person person) => $"{person.FirstName} {person.LastName}";
}

interface IStudentService
{
    string AsPersonText(Student student);
}

class StudentService(Func<Student, Person> map) : IStudentService
{
    public string AsPersonText(Student student)
    {
        var person = map(student);
        return person.Formatter?.Format(person) ?? "";
    }
}

partial class Program(ILogger logger, IStudentService studentService)
{
    public void Run()
    {
        var nik = new Student { FirstName = "John", LastName = "Smith" };
        var personText = studentService.AsPersonText(nik);
        logger.LogInformation(personText);
    }
}

partial class Composition
{
    private void Setup() =>

        DI.Setup(nameof(Composition))
            .Root<Program>(nameof(Root))
            .Arg<ICollection<string>>("logMessage")
            // Example dependency for Program
            .Bind().To<StudentService>()

            .DefaultLifetime(Singleton)
                // Example dependency for Person
                .Bind().To<PersonFormatter>()
                // Logger for AutoMapper
                .Bind().To<LoggerFactory>()
                .Bind().To((LoggerFactory loggerFactory) => loggerFactory.CreateLogger("info"))
                // Provides a mapper
                .Bind<IMapper>().To<LoggerFactory, Mapper>(loggerFactory => {
                    // Create the mapping configuration
                    var configuration = new MapperConfiguration(cfg => {
                            cfg.CreateMap<Student, Person>();
                        },
                        loggerFactory);
                    configuration.CompileMappings();
                    // Create the mapper
                    return new Mapper(configuration);
                })
                // Maps TT1 -> TT2
                .Bind().To<Func<TT1, TT2>>(ctx => source => {
                    ctx.Inject(out IMapper mapper);
                    // source -> target
                    var target = mapper.Map<TT1, TT2>(source);
                    // Building-up a mapped value with dependencies
                    ctx.BuildUp(target);
                    return target;
                });
}

class LoggerFactory(ICollection<string> logMessages)
    : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider) {}

    public ILogger CreateLogger(string categoryName) => new Logger(logMessages);

    public void Dispose() { }

    private class Logger(ICollection<string> logMessages): ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            logMessages.Add(formatter(state, exception));

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
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
  - [AutoMapper](https://www.nuget.org/packages/AutoMapper)
  - [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
  - [Pure.DI.Abstractions](https://www.nuget.org/packages/Pure.DI.Abstractions)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
dotnet add package AutoMapper
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Pure.DI.Abstractions
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

>[!NOTE]
>Since the `IMapper` binding is a singleton, the mapping configuration is created and compiled only once and then reused for all mappings.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition: IDisposable
{
  private Composition _root;
#if NET9_0_OR_GREATER
  private readonly Lock _lock;
#else
  private readonly Object _lock;
#endif
  private object[] _disposables;
  private int _disposeIndex;

  private Microsoft.Extensions.Logging.ILogger? _singletonILogger75;
  private Func<Student, Person>? _singletonFunc2147481193;
  private AutoMapper.Mapper? _singletonMapper76;
  private PersonFormatter? _singletonPersonFormatter73;
  private LoggerFactory? _singletonLoggerFactory74;

  private readonly ICollection<string> _argLogMessage;

  [OrdinalAttribute(128)]
  public Composition(ICollection<string> logMessage)
  {
    _argLogMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
    _root = this;
#if NET9_0_OR_GREATER
    _lock = new Lock();
#else
    _lock = new Object();
#endif
    _disposables = new object[1];
  }

  public Program Root
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      var root = _root ?? this;
      if (root._singletonFunc2147481193 is null)
        lock (_lock)
          if (root._singletonFunc2147481193 is null)
          {
            root._singletonFunc2147481193 = source =>
            {
              if (root._singletonMapper76 is null)
              {
                EnsureLoggerFactoryExists();
                LoggerFactory localLoggerFactory = root._singletonLoggerFactory74;
                // Create the mapping configuration
                var localConfiguration = new MapperConfiguration(cfg =>
                {
                  cfg.CreateMap<Student, Person>();
                }, localLoggerFactory);
                localConfiguration.CompileMappings();
                // Create the mapper
                root._singletonMapper76 = new Mapper(localConfiguration);
              }

              AutoMapper.IMapper localMapper = root._singletonMapper76;
              // source -> target
              Person localTarget = localMapper.Map<Student, Person>(source);
              // Building-up a mapped value with dependencies
              if (root._singletonPersonFormatter73 is null)
              {
                root._singletonPersonFormatter73 = new PersonFormatter();
              }

              localTarget.Formatter = root._singletonPersonFormatter73;
              return localTarget;
            };
          }

      if (root._singletonILogger75 is null)
        lock (_lock)
          if (root._singletonILogger75 is null)
          {
            EnsureLoggerFactoryExists();
            LoggerFactory localLoggerFactory1 = root._singletonLoggerFactory74;
            root._singletonILogger75 = localLoggerFactory1.CreateLogger("info");
          }

      return new Program(root._singletonILogger75, new StudentService(root._singletonFunc2147481193));
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      void EnsureLoggerFactoryExists()
      {
        if (root._singletonLoggerFactory74 is null)
          lock (_lock)
            if (root._singletonLoggerFactory74 is null)
            {
              root._singletonLoggerFactory74 = new LoggerFactory(_argLogMessage);
              _root._disposables[_root._disposeIndex++] = root._singletonLoggerFactory74;
            }
      }
    }
  }

  public void Dispose()
  {
    int disposeIndex;
    object[] disposables;
    lock (_lock)
    {
      disposeIndex = _disposeIndex;
      _disposeIndex = 0;
      disposables = _disposables;
      _disposables = new object[1];
      _singletonILogger75 = null;
      _singletonFunc2147481193 = null;
      _singletonMapper76 = null;
      _singletonPersonFormatter73 = null;
      _singletonLoggerFactory74 = null;
    }

    while (disposeIndex-- > 0)
    {
      switch (disposables[disposeIndex])
      {
        case IDisposable disposableInstance:
          try
          {
            disposableInstance.Dispose();
          }
          catch (Exception exception)
          {
            OnDisposeException(disposableInstance, exception);
          }
          break;
      }
    }
  }

  partial void OnDisposeException<T>(T disposableInstance, Exception exception) where T : IDisposable;
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
	Composition --|> IDisposable
	StudentService --|> IStudentService
	PersonFormatter --|> IPersonFormatter
	LoggerFactory --|> ILoggerFactory
	Mapper --|> IMapper
	Composition ..> Program : Program Root
	StudentService o-- "Singleton" FuncᐸStudentˏPersonᐳ : FuncᐸStudentˏPersonᐳ
	LoggerFactory o-- ICollectionᐸStringᐳ : Argument "logMessage"
	ILogger o-- "Singleton" LoggerFactory : LoggerFactory
	Mapper o-- "Singleton" LoggerFactory : LoggerFactory
	Program *-- StudentService : IStudentService
	Program o-- "Singleton" ILogger : ILogger
	FuncᐸStudentˏPersonᐳ o-- "Singleton" PersonFormatter : IPersonFormatterɁ
	FuncᐸStudentˏPersonᐳ o-- "Singleton" Mapper : IMapper
	namespace AutoMapper {
		class IMapper {
			<<interface>>
		}
		class Mapper {
				<<class>>
		}
	}
	namespace Microsoft.Extensions.Logging {
		class ILogger {
				<<interface>>
		}
		class ILoggerFactory {
			<<interface>>
		}
	}
	namespace Pure.DI.UsageTests.UseCases.AutoMapperScenario {
		class Composition {
		<<partial>>
		+Program Root
		}
		class IPersonFormatter {
			<<interface>>
		}
		class IStudentService {
			<<interface>>
		}
		class LoggerFactory {
				<<class>>
			+LoggerFactory(ICollectionᐸStringᐳ logMessages)
		}
		class PersonFormatter {
				<<class>>
			+PersonFormatter()
		}
		class Program {
				<<class>>
			+Program(ILogger logger, IStudentService studentService)
		}
		class StudentService {
				<<class>>
			+StudentService(FuncᐸStudentˏPersonᐳ map)
		}
	}
	namespace System {
		class FuncᐸStudentˏPersonᐳ {
				<<delegate>>
			+IPersonFormatterɁ Formatter
		}
		class IDisposable {
			<<interface>>
		}
	}
	namespace System.Collections.Generic {
		class ICollectionᐸStringᐳ {
				<<interface>>
		}
	}
```

See also:

- [Build up of an existing object](build-up-of-an-existing-object.md)

