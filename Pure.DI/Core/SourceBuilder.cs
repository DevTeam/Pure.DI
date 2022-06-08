// ReSharper disable InvertIf
// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.IO;
using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal class SourceBuilder : ISourceBuilder
{
    private readonly IBuildContext _context;
    private readonly IMetadataBuilder _metadataBuilder;
    private readonly ISettings _settings;
    private readonly IFileSystem _fileSystem;
    private readonly IDiagnostic _diagnostic;
    private readonly ILog<SourceBuilder> _log;
    private readonly Func<IClassBuilder> _resolverBuilderFactory;

    public SourceBuilder(
        ILog<SourceBuilder> log,
        IBuildContext context,
        IMetadataBuilder metadataBuilder,
        ISettings settings,
        IFileSystem fileSystem,
        IDiagnostic diagnostic,
        Func<IClassBuilder> resolverBuilderFactory)
    {
        _context = context;
        _metadataBuilder = metadataBuilder;
        _settings = settings;
        _fileSystem = fileSystem;
        _diagnostic = diagnostic;
        _log = log;
        _resolverBuilderFactory = resolverBuilderFactory;
    }

    public void Build(IExecutionContext executionContext)
    {
        var isApiAvailable = IsApiAvailable(executionContext.Compilation);
        Defaults.DefaultNamespace = 
            executionContext.TryGetOption("build_property.PureDINamespace", out var newNamespace)
                ? newNamespace
                : string.Empty;

        var context = _metadataBuilder.Build(executionContext);
        
        if (executionContext.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!context.BaseMetadata.Any())
        {
            // ReSharper disable once StringLiteralTypo
            var error = $"Pure.DI API in the namespace \"{Defaults.DefaultNamespace}\" conflicts with an existing code. Please consider changing the namespace: https://github.com/DevTeam/Pure.DI#changing-the-puredi-api-namespace";
            _diagnostic.Error(Diagnostics.Error.InvalidSetup, error);
            throw new HandledException(error);
        }

        // Init context
        var curId = 0;
        _context.Initialize(curId++, context.Compilation, context.CancellationToken, context.BaseMetadata.First());

        if (!isApiAvailable)
        {
            foreach (var source in context.Api)
            {
                executionContext.AddSource(source.HintName, source.Code);
            }
        }

        var allMetadata = context.BaseMetadata.Concat(context.CurrentMetadata).ToList();
        Stopwatch stopwatch = new();
        Process? traceProcess = default;
        foreach (var rawMetadata in context.CurrentMetadata)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                stopwatch.Start();
                var semanticModel = context.Compilation.GetSemanticModel(rawMetadata.SetupNode.SyntaxTree);
                var metadata = CreateMetadata(rawMetadata, allMetadata);
                _context.Initialize(curId++, context.Compilation, context.CancellationToken, metadata);
                if (_settings.Debug)
                {
                    if (!Debugger.IsAttached)
                    {
                        _log.Info(() => new[]
                        {
                            "Launch a debugger."
                        });

                        Debugger.Launch();
                    }
                    else
                    {
                        _log.Info(() => new[]
                        {
                            "A debugger is already attached"
                        });
                    }
                }

                if (traceProcess == default && _settings.Trace && _settings.TryGetOutputPath(out var outputPath))
                {
                    var traceFile = Path.Combine(outputPath, $"dotTrace{Guid.NewGuid().ToString().Substring(0, 4)}.dtt");
                    var profiler = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools", "dottrace.exe");
                    traceProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo(
                            profiler,
                            $@"attach {Process.GetCurrentProcess().Id} --save-to=""{traceFile}"" --profiling-type=Sampling --timeout=30s")
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true
                        }
                    };

                    traceProcess.Start();
                    Thread.Sleep(2000);
                }

                _log.Info(() =>
                {
                    var messages = new List<string>
                    {
                        $"Processing {rawMetadata.ComposerTypeName}",
                        $"{nameof(DI)}.{nameof(DI.Setup)}()"
                    };

                    messages.AddRange(metadata.Bindings.Select(binding => $"  .{binding}"));
                    return messages.ToArray();
                });

                var compilationUnitSyntax = _resolverBuilderFactory().Build(semanticModel);
                var source = new Source($"{metadata.ComposerTypeName}.cs", SourceText.From(compilationUnitSyntax.ToFullString(), Encoding.UTF8));
                if (_settings.TryGetOutputPath(out outputPath))
                {
                    _log.Info(() => new[]
                    {
                        $"The output path: {outputPath}"
                    });

                    _fileSystem.WriteFile(Path.Combine(outputPath, source.HintName), source.Code.ToString());
                }

                stopwatch.Stop();
                _log.Info(() => new[]
                {
                    $"Initialize {context.InitDurationMilliseconds} ms.", $"Generate {stopwatch.ElapsedMilliseconds} ms."
                });

                executionContext.AddSource(source.HintName, source.Code);
            }
            catch (BuildException buildException)
            {
                _diagnostic.Error(buildException.Id, buildException.Message, buildException.Locations);
            }
            catch (HandledException)
            {
            }
        }
    }

    private static ResolverMetadata CreateMetadata(ResolverMetadata metadata, IReadOnlyCollection<ResolverMetadata> allMetadata)
    {
        var newMetadata = new ResolverMetadata(metadata.SetupNode, metadata.ComposerTypeName, metadata.Owner);
        var dependencies = GetDependencies(metadata, new HashSet<ResolverMetadata>(), allMetadata);
        foreach (var dependency in dependencies)
        {
            newMetadata.Merge(dependency);
        }

        newMetadata.Merge(metadata);
        return newMetadata;
    }

    private static IEnumerable<ResolverMetadata> GetDependencies(ResolverMetadata metadata, ISet<ResolverMetadata> processed, IReadOnlyCollection<ResolverMetadata> allMetadata)
    {
        var dependencies =
            from dependencyName in metadata.DependsOn
            from dependency in allMetadata
            where dependencyName.Equals(dependency.ComposerTypeName, StringComparison.InvariantCultureIgnoreCase)
            select dependency;

        foreach (var dependency in dependencies)
        {
            if (!processed.Add(dependency))
            {
                continue;
            }

            yield return dependency;

            foreach (var nested in GetDependencies(dependency, processed, allMetadata))
            {
                yield return nested;
            }
        }
    }
    
    private static bool IsApiAvailable(Compilation compilation)
    {
        var diType = compilation.GetTypesByMetadataName(typeof(DI).FullName.ReplaceNamespace()).FirstOrDefault();
        if (diType == null)
        {
            return false;
        }

        var type = (
            from tree in compilation.SyntaxTrees
            let semanticModel = compilation.GetSemanticModel(tree)
            from typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            let symbol = ModelExtensions.GetDeclaredSymbol(semanticModel, typeDeclaration)
            select symbol).FirstOrDefault();

        return type != null && compilation.IsSymbolAccessibleWithin(diType, type);
    }
}