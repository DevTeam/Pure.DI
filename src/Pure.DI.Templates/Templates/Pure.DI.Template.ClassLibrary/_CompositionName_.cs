namespace _PureDIProjectName_;

internal class $(CompositionName)
{
    private void Setup() => DI.Setup(kind: CompositionKind.Global)
        .Bind().As(Singleton).To<ConsoleAdapter>();
}