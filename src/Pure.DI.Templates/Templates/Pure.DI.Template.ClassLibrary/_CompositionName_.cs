#if (compositionName == 'IInput')
#error The composition name 'IInput' is not usable: the template already declares a type with that name. Choose another name, for example 'Composition'.
#endif
#if (compositionName == 'IOutput')
#error The composition name 'IOutput' is not usable: the template already declares a type with that name. Choose another name, for example 'Composition'.
#endif
#if (compositionName == 'ConsoleAdapter')
#error The composition name 'ConsoleAdapter' is not usable: the template already declares a type with that name. Choose another name, for example 'Composition'.
#endif
using Pure.DI;

namespace _PureDIProjectName_;

/// <summary>
/// The library composition root. Declared with <see cref="CompositionKind.Global"/>, its bindings are shared with
/// every other composition in the project, letting Pure.DI wire the whole object graph at compile time.
/// </summary>
/// <remarks>
/// This class is packed into the library package as a source file, so a consumer compiles it in its own project.
/// Pure.DI is a compile-time generator and is not a dependency of the package: the consumer has to refer to Pure.DI
/// itself, which also lets the consumer add its own bindings to this global composition.
/// </remarks>
internal partial class $(CompositionName)
{
    /// <summary>
    /// Describes the composition: binds abstractions to implementations. Add further <c>Bind()</c> calls
    /// here as the library grows.
    /// </summary>
    private void Setup() => DI.Setup(kind: CompositionKind.Global)
        .Bind().As(Lifetime.Singleton).To<ConsoleAdapter>();
}
