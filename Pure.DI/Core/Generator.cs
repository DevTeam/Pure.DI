// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class Generator : IGenerator
{
    private readonly IMetadataBuilder _metadataBuilder;
    private readonly ISourceBuilder _sourceBuilder;
    private readonly ISettings _settings;
    private readonly IDiagnostic _diagnostic;

    public Generator(
        IMetadataBuilder metadataBuilder,
        ISourceBuilder sourceBuilder,
        ISettings settings,
        IDiagnostic diagnostic)
    {
        _metadataBuilder = metadataBuilder;
        _sourceBuilder = sourceBuilder;
        _settings = settings;
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
            // Debugger.Launch();
            Defaults.DefaultNamespace = context.TryGetOption("build_property.PureDINamespace", out var newNamespace) ? newNamespace : string.Empty;

            var metadata = _metadataBuilder.Build(context);
            if (metadata.BaseMetadata.Count == 0)
            {
                _diagnostic.Error(Diagnostics.Error.InvalidSetup, "DI Setup is incorrect.");
            }

            try
            {
                foreach (var source in _sourceBuilder.Build(metadata))
                {
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    context.AddSource(source.HintName, source.Code);
                }
            }
            catch (BuildException buildException)
            {
                _diagnostic.Error(buildException.Id, buildException.Message, buildException.Locations);
            }
            catch (HandledException)
            {
            }

            if (_settings.Api)
            {
                foreach (var source in metadata.Api)
                {
                    context.AddSource(source.HintName, source.Code);
                }
            }
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