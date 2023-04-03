using Pure.DI;
using static Pure.DI.Lifetime;

namespace _PureDIProjectName_;

/// <summary>
/// This class is used to resolve a composition root.
/// <example>
/// <code>
/// new $(CompositionName)().Root;
/// </code>
/// </example>
/// </summary>
internal static partial class $(CompositionName)
{
    // ReSharper disable once UnusedMember.Local
    /// <summary>
    /// It is never called, only for DI setup. For more details, please see: https://github.com/DevTeam/Pure.DI
    /// </summary>
    private static void Setup() => DI.Setup()
        .Bind<IInput>().Bind<IOutput>().As(Singleton).To<ConsoleAdapter>()
        .Root<Program>("Root");
}
