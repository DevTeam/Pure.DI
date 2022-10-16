using Pure.DI;
using static Pure.DI.Lifetime;

namespace _PureDIProjectName_;

/// <summary>
/// This static class is used to resolve a composition root.
/// <example>
/// <code>
/// $(ComposerName).Resolve&lt;Program&gt;();
/// </code>
/// </example>
/// </summary>
internal static partial class $(ComposerName)
{
    // ReSharper disable once UnusedMember.Local
    /// <summary>
    /// It is never called, only for DI setup. For more details, please see: https://github.com/DevTeam/Pure.DI
    /// </summary>
    private static void Setup() => DI.Setup()
        .Root<Program>()
        .Bind<IInput>().Bind<IOutput>().As(Singleton).To<ConsoleAdapter>();
}
