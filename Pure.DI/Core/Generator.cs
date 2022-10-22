// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class Generator : IGenerator
{
    private readonly ISourceBuilder _sourceBuilder;
    private readonly IDiagnostic _diagnostic;

    public Generator(
        ISourceBuilder sourceBuilder,
        IDiagnostic diagnostic)
    {
        _sourceBuilder = sourceBuilder;
        _diagnostic = diagnostic;
    }

    public void Generate(IExecutionContext context)
    {
        if (_diagnostic is CompilationDiagnostic compilationDiagnostic)
        {
            compilationDiagnostic.Context = context;
        }

        try
        {
            _sourceBuilder.Build(context);
        }
        catch (BuildException buildException)
        {
            _diagnostic.Error(buildException.Id, buildException.Message, buildException.Locations);
        }
        catch (HandledException)
        {
        }
        catch (Exception ex)
        {
            _diagnostic.Error(Diagnostics.Error.Unhandled, ex.ToString().Replace(Environment.NewLine, " "));
        }
    }
}