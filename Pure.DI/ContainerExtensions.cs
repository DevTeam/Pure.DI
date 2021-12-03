namespace Pure.DI
{
    using Core;
    using IoC;

    internal static class ContainerExtensions
    {
        public static IContainer Create() =>
            Container
            .Create()
            .Using<Configuration>()
            .Create()
            .Bind<CompilationDiagnostic>().Bind<IDiagnostic>().As(IoC.Lifetime.ContainerSingleton).To<CompilationDiagnostic>()
            .Container;
    }
}