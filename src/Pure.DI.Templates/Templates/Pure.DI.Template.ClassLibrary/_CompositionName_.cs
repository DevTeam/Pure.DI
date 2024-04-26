namespace _PureDIProjectName_;

internal class $(CompositionName)
{
    private void Setup() => 
        DI.Setup(nameof($(CompositionName)), CompositionKind.Global)
            .Bind().As(Singleton).To<ConsoleAdapter>();
}