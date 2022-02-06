// ReSharper disable InvertIf
// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.IO;
using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal class SourceBuilder : ISourceBuilder
{
    private readonly IBuildContext _context;
    private readonly ISettings _settings;
    private readonly IFileSystem _fileSystem;
    private readonly ILog<SourceBuilder> _log;
    private readonly Func<IClassBuilder> _resolverBuilderFactory;

    public SourceBuilder(
        ILog<SourceBuilder> log,
        IBuildContext context,
        ISettings settings,
        IFileSystem fileSystem,
        Func<IClassBuilder> resolverBuilderFactory)
    {
        _context = context;
        _settings = settings;
        _fileSystem = fileSystem;
        _log = log;
        _resolverBuilderFactory = resolverBuilderFactory;
    }

    public IEnumerable<Source> Build(MetadataContext context)
    {
        var allMetadata = context.BaseMetadata.Concat(context.CurrentMetadata).ToList();

        // Init context
        var curId = 0;
        _context.Initialize(curId++, context.Compilation, context.CancellationToken, allMetadata.First());

        Stopwatch stopwatch = new();
        stopwatch.Start();
        Process? traceProcess = default;
        foreach (var rawMetadata in context.CurrentMetadata)
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

            var compilationUnitSyntax = _resolverBuilderFactory().Build(semanticModel).NormalizeWhitespace();
            var source = new Source($"{metadata.ComposerTypeName}.cs", SourceText.From(compilationUnitSyntax.ToFullString().ReplaceNamespace(), Encoding.UTF8));
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

            yield return source;
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
}