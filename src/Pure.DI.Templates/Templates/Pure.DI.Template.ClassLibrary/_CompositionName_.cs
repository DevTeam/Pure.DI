namespace _PureDIProjectName_;

internal class $(CompositionName)
{
    [Conditional("DI")]
    private void Setup() => 
        DI.Setup(nameof($(CompositionName)), CompositionKind.Global)
            .Bind().As(Singleton).To<ConsoleAdapter>();
}