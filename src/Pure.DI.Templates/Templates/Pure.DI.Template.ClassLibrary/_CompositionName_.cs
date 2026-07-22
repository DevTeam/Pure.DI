using Pure.DI;
using static Pure.DI.CompositionKind;
using static Pure.DI.Lifetime;

namespace _PureDIProjectName_;

/// <summary>
/// The library composition root. Declared with <c>CompositionKind.Global</c>, its bindings are shared with
/// every other composition in the project, letting Pure.DI wire the whole object graph at compile time.
/// </summary>
internal class $(CompositionName)
{
    /// <summary>
    /// Describes the composition: binds abstractions to implementations. Add further <c>Bind()</c> calls
    /// here as the library grows.
    /// </summary>
    private void Setup() => DI.Setup(kind: Global)
        .Bind().As(Singleton).To<ConsoleAdapter>();
}
