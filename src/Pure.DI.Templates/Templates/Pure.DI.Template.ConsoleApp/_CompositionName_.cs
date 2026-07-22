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