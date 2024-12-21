namespace _PureDIProjectName_;

internal class $(CompositionName)
{
    private void Setup() => DI.Setup(kind: Global)
        .Bind().As(Singleton).To<ConsoleAdapter>();
}