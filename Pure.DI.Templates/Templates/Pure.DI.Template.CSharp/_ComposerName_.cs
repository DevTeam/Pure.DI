using Pure.DI;
using static Pure.DI.Lifetime;

namespace _PureDIProjectName_
{
    /// <summary>
    /// This static class is used to resolve a composition root.
    /// <example>
    /// <code>
    /// $(ComposerName).Resolve<Program>();
    /// </code>
    /// </example>
    /// </summary>
    internal static partial class $(ComposerName)
    {
        /// <summary>
        /// It is never called, only for DI setup. For more details, please see: https://github.com/DevTeam/Pure.DI
        /// </summary>
        private static void Setup() => DI.Setup()
            .Bind<Program>().As(Singleton).To<Program>()
            .Bind<IInput>().Bind<IOutput>().As(Singleton).To<ConsoleAdapter>();
    }
}
