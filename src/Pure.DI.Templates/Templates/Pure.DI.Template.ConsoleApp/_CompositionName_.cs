namespace _PureDIProjectName_;

internal partial class $(CompositionName)
{
    private void Setup() => 
        DI.Setup(nameof($(CompositionName)))
            .Bind().As(Singleton).To<ConsoleAdapter>()
            .Root<Program>("Root");
}