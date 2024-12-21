namespace _PureDIProjectName_;

internal partial class $(CompositionName)
{
    private void Setup() => DI.Setup()
        .Bind().As(Singleton).To<ConsoleAdapter>()
        .Root<Program>(nameof(Root));
}