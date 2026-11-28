#if (compositionName == 'Root')
#error The composition name 'Root' is not usable: Pure.DI generates a root member with that name, so the member would clash with its enclosing type. Choose another name, for example 'Composition'.
#endif
#if (compositionName == 'Program')
#error The composition name 'Program' is not usable: the template already declares a type with that name. Choose another name, for example 'Composition'.
#endif
#if (compositionName == 'IInput')
#error The composition name 'IInput' is not usable: the template already declares a type with that name. Choose another name, for example 'Composition'.
#endif
#if (compositionName == 'IOutput')
#error The composition name 'IOutput' is not usable: the template already declares a type with that name. Choose another name, for example 'Composition'.
#endif
#if (compositionName == 'ConsoleAdapter')
#error The composition name 'ConsoleAdapter' is not usable: the template already declares a type with that name. Choose another name, for example 'Composition'.
#endif
namespace _PureDIProjectName_;

/// <summary>
/// The application composition root. Pure.DI generates the object graph from the bindings declared in
/// <see cref="Setup"/> at compile time, so no reflection or runtime container is involved.
/// </summary>
internal partial class $(CompositionName)
{
    /// <summary>
    /// Describes the composition: binds abstractions to implementations and exposes <see cref="Program"/>
    /// as the composition root named <c>Root</c>. Add further <c>Bind()</c> calls here as the app grows.
    /// </summary>
    private void Setup() => DI.Setup()
        .Bind().As(Singleton).To<ConsoleAdapter>()
        .Root<Program>(nameof(Root));
}
