namespace Pure.DI
{
    using System;
    using System.Diagnostics;
    using Core;
    using IoC;
    using Microsoft.CodeAnalysis;

    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        private static readonly IMutableContainer Container = IoC.Container
            .Create()
            .Using<Configuration>()
            .Create()
            .Bind<CompilationDiagnostic>().Bind<IDiagnostic>().As(IoC.Lifetime.ContainerSingleton).To<CompilationDiagnostic>()
            .Container;

        private static readonly Func<IGenerator> CreateGenerator = Container.Resolve<Func<IGenerator>>();

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG && AA
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
        }

        public void Execute(GeneratorExecutionContext context) => CreateGenerator().Generate(context);
    }
}