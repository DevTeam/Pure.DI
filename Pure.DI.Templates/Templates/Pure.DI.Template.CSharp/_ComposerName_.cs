using Pure.DI;
using static Pure.DI.Lifetime;

namespace _PureDIProjectName_
{
    internal static partial class $(ComposerName)
    {
        static $(ComposerName)() => DI.Setup()
            .Bind<Program>().As(Singleton).To<Program>()
            .Bind<IStdIn>().Bind<IStdOut>().As(Singleton).To<InOut>();
    }
}
