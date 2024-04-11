namespace _PureDIProjectName_;

internal partial class $(CompositionName)
{
    [Conditional("DI")]
    private void Setup() => 
        DI.Setup(nameof($(CompositionName)))
            .Arg<string[]>("args")
            .Bind().As(Singleton).To<ConsoleAdapter>()
            .Root<Program>("Root");
}