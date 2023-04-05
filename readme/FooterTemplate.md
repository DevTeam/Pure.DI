## Composition class

For each generated class, hereinafter referred to as _composition_, the setup must be done:

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>();
```

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

<details>
<summary>The second optional parameter</summary>

The second optional parameter can have several values to determine the kind of composition:

| Options                  |                                                                                                                                              |
|--------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| CompositionKind.Public   | Default value. This option will create a composition class.                                                                                  |
| CompositionKind.Internal | If this value is specified, the class will not be generated, but this setup can be used for others as a base.                                |
| CompositionKind.Global   | If this value is specified, the composition class will not be generated, but this setup is a default base for all setups in current project. |

</details>

The composition may contain the following parts:

### Resolve Methods

By default a set of four _Resolve_ methods are generated within generated composition class.

```c#
public T Resolve<T>() { ... }

public T Resolve<T>(object? tag) { ... }

public object Resolve(Type type) { ... }

public object Resolve(Type type, object? tag) { ... }
```

These methods are useful when using the Service Locator approach when the code resolves composition roots in place:

```c#
var composition = new Composition();

composition.Resolve<IService>();
```

To control the generation of these methods, see [Resolve Hint](#Resolve-Hint).

A set of private properties are generated.

### Private Roots

The composition has properties for each potential root that are used in those _Resolve_ methods. For example:

```c#
private IService Root2PropABB3D0
{
    get { ... }
}
```

This properties have a random name and a private accessor and cannot be used directly from code.

### Public Roots

To be able to use a specific composition root, that root must be explicitly defined by the _Root_ method:

```c#
DI.Setup("Composition")
    .Bind<IService>().To<Service>()
    .Root<IService>("MyService");
```

In this case, the property for type _IService_ will have a specific name and will be available for direct use. The result of its use will be the creation of a composition of objects with a root of type _IService_:

```c#
public IService MyService
{
    get
    { 
        ...
        retunr new Service(...);
    }
}
```

## Setup hints

Setup hints are comments before method _Setup_ in the form ```hint = value``` that are used to fine-tune code generation. For example:

```c#
// Resolve = Off
// ThreadSafe = Off
// ToString = On
DI.Setup("Composition")
    ...
```

<details>
<summary>Available hints</summary>

| Hint                                                                                                                               | Default Value |
|------------------------------------------------------------------------------------------------------------------------------------|---------------|
| [Resolve](#Resolve-Hint)                                                                                                           | On            |
| [OnInstanceCreation](#OnInstanceCreation-Hint)                                                                                     | On            |
| [OnDependencyInjection](#OnDependencyInjection-Hint)                                                                               | Off           |
| [OnDependencyInjectionImplementationTypeNameRegularExpression](#OnDependencyInjectionImplementationTypeNameRegularExpression-Hint) | .+            |
| [OnDependencyInjectionContractTypeNameRegularExpression](#OnDependencyInjectionContractTypeNameRegularExpression-Hint)             | .+            |
| [OnDependencyInjectionTagRegularExpression](#OnDependencyInjectionTagRegularExpression-Hint)                                       | .+            |
| [ToString](#ToString-Hint)                                                                                                         | Off           |
| [ThreadSafe](#ThreadSafe-Hint)                                                                                                     | On            |

### Resolve Hint

Determine whether to generate [_Resolve_ methods](#resolve-methods). By default a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce class composition generation time and no [private composition roots](#Private-Roots) will be generated in this case. The composition will be tiny and will only have [public roots](#Public-Roots).

### OnInstanceCreation Hint

Determine whether to generate partial _OnInstanceCreation_ method. This partial method is generated by default, has no body, and can be overridden as needed. If the body is not defined, then the compiler will cut out its calls. This can be useful, for example, for logging:

```c#
internal partial class Composition
{
    partial void OnInstanceCreation<T>(ref T value, object? tag, object? lifetime)            
    {
        Console.WriteLine($"'{typeof(T)}'('{tag}') created.");            
    }
}
```

You can also replace the created instance of type `T`, where `T` is actually type of created instance.

### OnDependencyInjection Hint

Determine whether to generate partial _OnDependencyInjection_ method to control of dependency injection. This partial method is not generated by default. It cannot have an empty body due to the return value. It must be overridden when generated. This can be useful, for example, for [interception](#Interception).

```c#
// OnDependencyInjection = On
// OnDependencyInjectionContractTypeNameRegularExpression = ICalculator[\d]{1}
// OnDependencyInjectionTagRegularExpression = Abc
DI.Setup("Composition")
    ...
```

To minimize the performance penalty when calling _OnDependencyInjection_, use the other hints below.

### OnDependencyInjectionImplementationTypeNameRegularExpression Hint

It is a regular expression to filter by the instance type name. This hint is useful when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of types for which the method _OnDependencyInjection_ will be called.

### OnDependencyInjectionContractTypeNameRegularExpression Hint

It is a regular expression to filter by the resolving type name. This hint is useful also when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of resolving types for which the method _OnDependencyInjection_ will be called.

### OnDependencyInjectionTagRegularExpression Hint

It is a regular expression to filter by the _tag_. This hint is useful also when _OnDependencyInjection_ is in the _On_ state and you want to limit the set of _tag_ for which the method _OnDependencyInjection_ will be called.

### ToString Hint

### ThreadSafe Hint

</details>
