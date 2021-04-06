namespace Pure.DI
{
    using System;
    using Core;
    using IoC;
    using Microsoft.CodeAnalysis;

    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        private readonly IMutableContainer _container = Container.Create().Using<Configuration>();

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG && AA
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var container = _container
                .Create()
                .Bind<IDiagnostic>().To(ctx => new CompilationDiagnostic(context))
                .Container;

            var sourceBuilder = container.Resolve<ISourceBuilder>();
            try
            {
                foreach (var source in sourceBuilder.Build(context.Compilation))
                {
                    context.AddSource(source.HintName, source.Code);
                }
            }
            catch (HandledException)
            {
            }
            catch (Exception ex)
            {
                container
                    .Resolve<IDiagnostic>()
                    .Error(Diagnostics.Unhandled, ex.ToString());
            }
        }
    }
}