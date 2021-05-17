namespace Pure.DI
{
    using Core;
    using IoC;
    using Microsoft.CodeAnalysis;

    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        private static readonly IContainer GeneratorContainer = Container
                .Create()
                .Using<Configuration>()
                .Create()
                .Bind<CompilationDiagnostic>().Bind<IDiagnostic>().As(IoC.Lifetime.ContainerSingleton).To<CompilationDiagnostic>()
                .Container;

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG && AA
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
        }

        public void Execute(GeneratorExecutionContext context) =>
            GeneratorContainer.Create().Resolve<IGenerator>().Generate(context);
    }
}