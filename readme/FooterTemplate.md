### Applications
- [Console application](readme/ConsoleTemplate.md)
- [WPF application](readme/WpfTemplate.md)
- [Top level statements console application](readme/ConsoleTopLevelStatementsTemplate.md)

## Generated Code

Each generated class, hereinafter referred to as _composition_, needs to be configured. It starts with the `Setup(...)` method:

```c#
DI.Setup("Composition")
    .Bind<IDependency>().To<Dependency>()
    .Bind<IService>().To<Service>()
    .Root<IService>("Root");
```

<details>
<summary>The following class will be generated</summary>

```c#
partial class Composition
{
    public Composition() { }

    internal Composition(Composition parent) { }

    public IService Root
    {
        get
        {
            return new Service(new Dependency());
        }
    }

    public T Resolve<T>()  { ... }

    public T Resolve<T>(object? tag)  { ... }

    public object Resolve(System.Type type) { ... }

    public object Resolve(System.Type type, object? tag) { ... }
}
```

</details>

<details>
<summary>Setup arguments</summary>

The first parameter is used to specify the name of the composition class. All setups with the same name will be combined to create one composition class. In addition, this name may contain a namespace, for example for `Sample.Composition` the composition class is generated:

```c#
namespace Sample
{
    partial class Composition
    {
        ...
    }
}
```

The second optional parameter can have several values to determine the kind of composition.

### CompositionKind.Public

This is the default value. If this value is specified, a composition class will be created.

### CompositionKind.Internal

If this value is specified, the class will not be generated, but this setup can be used for others as a base. For example:

```c#
DI.Setup("BaseComposition", CompositionKind.Internal)
    .Bind<IDependency>().To<Dependency>();

DI.Setup("Composition").DependsOn("BaseComposition")
    .Bind<IService>().To<Service>();    
```

When the composition _CompositionKind.Public_ flag is set in the composition setup, it can also be the base composition for others like in the example above.

### CompositionKind.Global

If this value is specified, no composition class will be created, but this setup is the base for all setups in the current project, and `DependsOn(...)` is not required.

</details>

<details>
<summary>Constructors</summary>

### Default constructor

Everything is quite banal, this constructor simply initializes the internal state.

### Argument constructor

It replaces the default constructor and is only created if at least one argument is provided. For example:

```c#
DI.Setup("Composition")
    .Arg<string>("name")
    .Arg<int>("id")
    ...
```

In this case, the argument constructor looks like this:

```c#
public Composition(string name, int id) { ... }
```

and default constructor is missing.

### Child constructor

This constructor is always available and is used to create a child composition based on the parent composition:

```c#
var parentComposition = new Composition();
var childComposition = new Composition(parentComposition); 
```

The child composition inherits the state of the parent composition in the form of arguments and singleton objects. States are copied, and compositions are completely independent, except when calling the _Dispose()_ method on the parent container before disposing of the child container, because the child container can use singleton objects created before it was created.

</details>

<details>
<summary>Properties</summary>

To be able to quickly and conveniently create an object graph, a set of properties is generated. These properties are called compositions roots here. The type of the property is the type of a root object created by the composition. Accordingly, each access to the property leads to the creation of a composition with the root element of this type.

### Public Composition Roots

To be able to use a specific composition root, that root must be explicitly defined by the _Root_ method with a specific name and type:

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
```

In this case, the property for type _IService_ will be named _MyService_ and will be available for direct use. The result of its use will be the creation of a composition of objects with a root of type _IService_:

```c#
public IService MyService
{
    get
    { 
        ...
        return new Service(...);
    }
}
```

This is [recommended way](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) to create a composition root. A composition class can contain any number of roots.

### Private Composition Roots

When the root name is empty, a private composition root is created. This root is used in these _Resolve_ methods in the same way as public roots. For example:

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>();
```

```c#
private IService Root1ABB3D0
{
    get { ... }
}
```

These properties have a random name and a private accessor and cannot be used directly from code. Don't try to use them. Private composition roots can be resolved by the _Resolve_ methods.

</details>

<details>
<summary>Methods</summary>

### Resolve

By default a set of four _Resolve_ methods are generated:

```c#
public T Resolve<T>() { ... }

public T Resolve<T>(object? tag) { ... }

public object Resolve(Type type) { ... }

public object Resolve(Type type, object? tag) { ... }
```

These methods can resolve public composition roots as well as private roots and are useful when using the [Service Locator](https://martinfowler.com/articles/injection.html) approach when the code resolves composition roots in place:

```c#
var composition = new Composition();

composition.Resolve<IService>();
```

This is [not recommended](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) way to create composition roots. To control the generation of these methods, see the _Resolve_ hint.

### Dispose

Provides a mechanism for releasing unmanaged resources. This method is only generated if the composition contains at least one singleton instance that implements the [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable) interface. To dispose of all created singleton objects, call the composition `Dispose()` method:

```c#
using(var composition = new Composition())
{
    ...
}
```

</details>

<details>
<summary>Setup hints</summary>

## Setup hints

Hints are used to fine-tune code generation. Setup hints can be used as in the following example:

```c#
DI.Setup("Composition")
    .Hint(Hint.Resolve, "Off")
    .Hint(Hint.ThreadSafe, "Off")
    .Hint(Hint.ToString, "On")
    ...
```

In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example:

```c#
// Resolve = Off
// ThreadSafe = Off
DI.Setup("Composition")
    .Hint(Hint.ToString, "On")
    ...
```

| Hint                                                                                                                               | Values             | Default   | C# version |
|------------------------------------------------------------------------------------------------------------------------------------|--------------------|-----------|------------|
| [Resolve](#Resolve-Hint)                                                                                                           | _On_ or _Off_      | _On_      |            |
| [OnInstanceCreation](#OnInstanceCreation-Hint)                                                                                     | _On_ or _Off_      | _Off_     | 9.0        |
| [OnInstanceCreationImplementationTypeNameRegularExpression](#OnInstanceCreationImplementationTypeNameRegularExpression-Hint)       | Regular expression | .+        |            |
| [OnInstanceCreationTagRegularExpression](#OnInstanceCreationTagRegularExpression-Hint)                                             | Regular expression | .+        |            |
| [OnInstanceCreationLifetimeRegularExpression](#OnInstanceCreationLifetimeRegularExpression-Hint)                                   | Regular expression | .+        |            |
| [OnDependencyInjection](#OnDependencyInjection-Hint)                                                                               | _On_ or _Off_      | _Off_     | 9.0        |
| [OnDependencyInjectionImplementationTypeNameRegularExpression](#OnDependencyInjectionImplementationTypeNameRegularExpression-Hint) | Regular expression | .+        |            |
| [OnDependencyInjectionContractTypeNameRegularExpression](#OnDependencyInjectionContractTypeNameRegularExpression-Hint)             | Regular expression | .+        |            |
| [OnDependencyInjectionTagRegularExpression](#OnDependencyInjectionTagRegularExpression-Hint)                                       | Regular expression | .+        |            |
| [OnDependencyInjectionLifetimeRegularExpression](#OnDependencyInjectionLifetimeRegularExpression-Hint)                             | Regular expression | .+        |            |
| [OnCannotResolve](#OnCannotResolve-Hint)                                                                                           | _On_ or _Off_      | _Off_     | 9.0        |
| [OnCannotResolveContractTypeNameRegularExpression](#OnCannotResolveContractTypeNameRegularExpression-Hint)                         | Regular expression | .+        |            |
| [OnCannotResolveTagRegularExpression](#OnCannotResolveTagRegularExpression-Hint)                                                   | Regular expression | .+        |            |
| [OnCannotResolveLifetimeRegularExpression](#OnCannotResolveLifetimeRegularExpression-Hint)                                         | Regular expression | .+        |            |
| [ToString](#ToString-Hint)                                                                                                         | _On_ or _Off_      | _Off_     |            |
| [ThreadSafe](#ThreadSafe-Hint)                                                                                                     | _On_ or _Off_      | _On_      |            |
| [ResolveMethodModifiers](#ResolveMethodModifiers-Hint)                                                                             | Method modifier    | _public_  |            |
| [ResolveMethodName](#ResolveMethodName-Hint)                                                                                       | Method name        | _Resolve_ |            |
| [ResolveByTagMethodModifiers](#ResolveByTagMethodModifiers-Hint)                                                                   | Method modifier    | _public_  |            |
| [ResolveByTagMethodName](#ResolveByTagMethodName-Hint)                                                                             | Method name        | _Resolve_ |            |
| [ObjectResolveMethodModifiers](#ObjectResolveMethodModifiers-Hint)                                                                 | Method modifier    | _public_  |            |
| [ObjectResolveMethodName](#ObjectResolveMethodName-Hint)                                                                           | Method name        | _Resolve_ |            |
| [ObjectResolveByTagMethodModifiers](#ObjectResolveByTagMethodModifiers-Hint)                                                       | Method modifier    | _public_  |            |
| [ObjectResolveByTagMethodName](#ObjectResolveByTagMethodName-Hint)                                                                 | Method name        | _Resolve_ |            |
| [DisposeMethodModifiers](#DisposeMethodModifiers-Hint)                                                                             | Method modifier    | _public_  |            |
| [FormatCode](#FormatCode-Hint)                                                                                                     | _On_ or _Off_      | _Off_     |            |

### Resolve Hint

Determines whether to generate [_Resolve_ methods](#resolve-methods). By default a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce class composition generation time and no [private composition roots](#Private-Roots) will be generated in this case. The composition will be tiny and will only have [public roots](#Public-Roots). When the _Resolve_ hint is disabled, only the public root properties are available, so be sure to define them explicitly with the `Root<T>(...)` method.

### OnInstanceCreation Hint

Determines whether to generate partial _OnInstanceCreation_ method. This partial method is not generated by default. This can be useful, for example, for logging:

```c#
internal partial class Composition
{
    partial void OnInstanceCreation<T>(ref T value, object? tag, object lifetime)            
    {
        Console.WriteLine($"'{typeof(T)}'('{tag}') created.");            
    }
}
```

You can also replace the created instance of type `T`, where `T` is actually type of created instance. To minimize the performance penalty when calling _OnInstanceCreation_, use the three related hints below.

### OnInstanceCreationImplementationTypeNameRegularExpression Hint

It is a regular expression to filter by the instance type name. This hint is useful when _OnInstanceCreation_ is in the _On_ state and you want to limit the set of types for which the method _OnInstanceCreation_ will be called.

### OnInstanceCreationTagRegularExpression Hint

It is a regular expression to filter by the _tag_. This hint is useful also when _OnInstanceCreation_ is in the _On_ state and you want to limit the set of _tag_ for which the method _OnInstanceCreation_ will be called.

### OnInstanceCreationLifetimeRegularExpression Hint

It is a regular expression to filter by the _lifetime_. This hint is useful also when _OnInstanceCreation_ is in the _On_ state and you want to limit the set of _lifetime_ for which the method _OnInstanceCreation_ will be called.

### OnDependencyInjection Hint

Determines whether to generate partial _OnDependencyInjection_ method to control of dependency injection. This partial method is not generated by default. It cannot have an empty body due to the return value. It must be overridden when generated. This can be useful, for example, for [interception](#Interception).

```c#
// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameRegularExpression = ICalculator[\d]{1}
// OnDependencyInjectionTagRegularExpression = Abc
DI.Setup("Composition")
    ...
```

To minimize the performance penalty when calling _OnDependencyInjection_, use the three related hints below.

### OnDependencyInjectionImplementationTypeNameRegularExpression Hint

It is a regular expression to filter by the instance type name. This hint is useful when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of types for which the method _OnDependencyInjection_ will be called.

### OnDependencyInjectionContractTypeNameRegularExpression Hint

It is a regular expression to filter by the resolving type name. This hint is useful also when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of resolving types for which the method _OnDependencyInjection_ will be called.

### OnDependencyInjectionTagRegularExpression Hint

It is a regular expression to filter by the _tag_. This hint is useful also when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of _tag_ for which the method _OnDependencyInjection_ will be called.

### OnDependencyInjectionLifetimeRegularExpression Hint

It is a regular expression to filter by the _lifetime_. This hint is useful also when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of _lifetime_ for which the method _OnDependencyInjection_ will be called.

### OnCannotResolve Hint

Determines whether to generate a partial `OnCannotResolve<T>(...)` method to handle a scenario where an instance which cannot be resolved. This partial method is not generated by default. It cannot have an empty body due to the return value. It must be overridden on creation.

```c#
// OnCannotResolve = On
// OnCannotResolveContractTypeNameRegularExpression = string|DateTime
// OnDependencyInjectionTagRegularExpression = null
DI.Setup("Composition")
    ...
```

To avoid missing bindings by mistake, use the two related hints below.

### OnCannotResolveContractTypeNameRegularExpression Hint

It is a regular expression to filter by the resolving type name. This hint is useful also when _OnCannotResolve_ is in the _On_ state and you want to limit the set of resolving types for which the method _OnCannotResolve_ will be called.

### OnCannotResolveTagRegularExpression Hint

It is a regular expression to filter by the _tag_. This hint is useful also when _OnCannotResolve_ is in the _On_ state and you want to limit the set of _tag_ for which the method _OnCannotResolve_ will be called.

### OnCannotResolveLifetimeRegularExpression Hint

It is a regular expression to filter by the _lifetime_. This hint is useful also when _OnCannotResolve_ is in the _On_ state and you want to limit the set of _lifetime_ for which the method _OnCannotResolve_ will be called.

### ToString Hint

Determine if the _ToString()_ method should be generated. This method provides a text-based class diagram in the format [mermaid](https://mermaid.js.org/). To see this diagram, just call the ToString method and copy the text to [this site](https://mermaid.live/).

```c#
// ToString = On
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
    
var composition = new Composition();
string classDiagram = composition.ToString(); 
```

### ThreadSafe Hint

This hint determines whether the composition of objects will be created in a thread-safe manner. The default value of this hint is _On_. It is good practice not to use threads when creating an object graph, in which case the hint can be disabled, resulting in a slight performance gain.

```c#
// ThreadSafe = Off
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
```

### ResolveMethodModifiers Hint

Overrides modifiers of the method `public T Resolve<T>()`.

### ResolveMethodName Hint

Overrides name of the method `public T Resolve<T>()`.

### ResolveByTagMethodModifiers Hint

Overrides modifiers of the method `public T Resolve<T>(object? tag)`.

### ResolveByTagMethodName Hint

Overrides name of the method `public T Resolve<T>(object? tag)`.

### ObjectResolveMethodModifiers Hint

Overrides modifiers of the method `public object Resolve(Type type)`.

### ObjectResolveMethodName Hint

Overrides name of the method `public object Resolve(Type type)`.

### ObjectResolveByTagMethodModifiers Hint

Overrides modifiers of the method `public object Resolve(Type type, object? tag)`.

### ObjectResolveByTagMethodName Hint

Overrides name of the method `public object Resolve(Type type, object? tag)`.

### DisposeMethodModifiers Hint

Overrides modifiers of the method `public void Dispose()`.

### FormatCode Hint

Specifies whether the generated code should be formatted. This option consumes a lot of CPU resources. This hint can be useful for examining the generated code or for giving presentations, for example.

</details>

## Development environment requirements

- [.NET SDK 6.0.4xx or newer](https://dotnet.microsoft.com/download/dotnet/6.0)
- [C# 8 or newer](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history#c-version-80)

## Supported frameworks

- [.NET and .NET Core](https://docs.microsoft.com/en-us/dotnet/core/) 1.0+
- [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) 1.0+
- [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [.NET Framework](https://docs.microsoft.com/en-us/dotnet/framework/) 2.0+
- [UWP/XBOX](https://docs.microsoft.com/en-us/windows/uwp/index)
- [.NET IoT](https://dotnet.microsoft.com/apps/iot)
- [Xamarin](https://dotnet.microsoft.com/apps/xamarin)
- [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf)
- [.NET Multi-platform App UI (MAUI)](https://docs.microsoft.com/en-us/dotnet/maui/)

## Project template

Install the DI template [Pure.DI.Templates](https://www.nuget.org/packages/Pure.DI.Templates)

```shell
dotnet new -i Pure.DI.Templates
```

Create a "Sample" console application from the template *__di__*

```shell
dotnet new di -o ./Sample
```

And run it

```shell
dotnet run --project Sample
```

Please see [this page](https://github.com/DevTeam/Pure.DI/wiki/Project-templates) for more details about the template.

## Troubleshooting

<details>
<summary>Generated files</summary>

You can set build properties to save the generated file and control where the generated files are stored. In a project file, add the <EmitCompilerGeneratedFiles> element to a <PropertyGroup>, and set its value to true. Build your project again. Now, the generated files are created under obj/Debug/netX.X/generated/Pure.DI/Pure.DI.SourceGenerator. The components of the path map to the build configuration, target framework, source generator project name, and fully qualified type name of the generator. You can choose a more convenient output folder by adding the <CompilerGeneratedFilesOutputPath> element to the application's project file. For example:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    
    <PropertyGroup>
        <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
        <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
    </PropertyGroup>
    
</Project>
```

</details>

<details>
<summary>Log files</summary>

You can set build properties to save the log file. In the project file, add a <PureDILogFile> element to the <PropertyGroup> and set the path to the log directory, and add the related element `<CompilerVisibleProperty Include="PureDILogFile" />` to the <ItemGroup> to make this property visible in the source generator. To change the log level, specify the same with the _PureDISeverity_ property, as in the example below:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <PureDILogFile>.logs\Pure.DI.log</PureDILogFile>
        <PureDISeverity>Info</PureDISeverity>
    </PropertyGroup>

    <ItemGroup>
        <CompilerVisibleProperty Include="PureDILogFile" />
        <CompilerVisibleProperty Include="PureDISeverity" />
    </ItemGroup>

</Project>
```

The _PureDISeverity_ property has several options available:

| Severity | Description                                                            |
|----------|------------------------------------------------------------------------|
| Hidden   | Debug information.                                                     |
| Info     | Information that does not indicate a problem (i.e. not prescriptive).  |
| Warning  | Something suspicious, but allowed. This is the default value.          |
| Error    | Something not allowed by the rules of the language or other authority. |

</details>