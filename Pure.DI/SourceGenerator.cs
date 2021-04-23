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
        private readonly CompilationDiagnostic _diagnostic;
        private readonly Func<ISourceBuilder> _builderFactory;

        public SourceGenerator()
        {
            var container = _container
                .Create()
                .Bind<CompilationDiagnostic>().Bind<IDiagnostic>().As(IoC.Lifetime.Singleton).To<CompilationDiagnostic>()
                .Container;

            _diagnostic = container.Resolve<CompilationDiagnostic>();
            _builderFactory = container.Resolve<Func<ISourceBuilder>>();
        }

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
            _diagnostic.Context = context;
            var sourceBuilder = _builderFactory();
            try
            {
                foreach (var source in sourceBuilder.Build(context.Compilation))
                {
                    context.AddSource(source.HintName, source.Code);
                    /*var fileName = @"C:\Projects\DevTeam\Pure.DI\out\" + source.HintName;
                    File.Delete(fileName);
                    File.AppendAllText(fileName, source.Code.ToString());*/
                }
            }
            catch (HandledException)
            {
            }
            catch (Exception ex)
            {
                _diagnostic.Error(Diagnostics.Unhandled, ex.ToString());
            }
        }
    }
}