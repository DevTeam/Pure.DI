// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class Generator : IGenerator
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
        var workingThread = new Thread(() => GenerateInternal(context), 0xff_ffff)
        {
            Name = "Pure.DI",
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };

        workingThread.Start();
        while (!workingThread.Join(10) && !context.CancellationToken.IsCancellationRequested)
        {
        }

        if (context.CancellationToken.IsCancellationRequested && !workingThread.Join(1))
        {
            workingThread.Abort();
        }
    }

    private void GenerateInternal(IExecutionContext context)
    {
        if (_diagnostic is CompilationDiagnostic compilationDiagnostic)
        {
            compilationDiagnostic.Context = context;
        }

        try
        {
            //Debugger.Launch();
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