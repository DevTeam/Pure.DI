// ReSharper disable InvertIf
// ReSharper disable InvertIf
namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SourceBuilder : ISourceBuilder
    {
        private readonly IBuildContext _context;
        private readonly ISettings _settings;
        private readonly IFileSystem _fileSystem;
        private readonly IDiagnostic _diagnostic;
        private readonly ILog<SourceBuilder> _log;
        private readonly Func<IClassBuilder> _resolverBuilderFactory;
        private readonly Func<SemanticModel, IMetadataWalker> _metadataWalkerFactory;
        private readonly ICache<LanguageVersion, SourceBuilderState> _stateCache;

        public SourceBuilder(
            IBuildContext context,
            ISettings settings,
            IFileSystem fileSystem,
            IDiagnostic diagnostic,
            ILog<SourceBuilder> log,
            Func<IClassBuilder> resolverBuilderFactory,
            Func<SemanticModel, IMetadataWalker> metadataWalkerFactory,
            [Tag(Tags.GlobalScope)] ICache<LanguageVersion, SourceBuilderState> stateCache)
        {
            _context = context;
            _settings = settings;
            _fileSystem = fileSystem;
            _diagnostic = diagnostic;
            _log = log;
            _resolverBuilderFactory = resolverBuilderFactory;
            _metadataWalkerFactory = metadataWalkerFactory;
            _stateCache = stateCache;
        }

        public IEnumerable<Source> Build(Compilation compilation, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            var sourceSet = GetSourceSet(compilation);
            foreach (var source in GetComponents(compilation))
            {
                yield return source;
            }
            
            var syntaxTreesCount = compilation.SyntaxTrees.Count();
            compilation = compilation.AddSyntaxTrees(sourceSet.ComponentsTrees.Concat(sourceSet.FeaturesTrees));
            var featuresTrees = compilation.SyntaxTrees.Skip(syntaxTreesCount);
            var featuresMetadata = GetMetadata(compilation, featuresTrees, cancellationToken).ToList();
            foreach (var metadata in featuresMetadata)
            {
                metadata.DependsOn.Clear();
            }

            var currentTrees = compilation.SyntaxTrees.Take(syntaxTreesCount);
            var currentMetadata = GetMetadata(compilation, currentTrees, cancellationToken).ToList();
            var allMetadata = featuresMetadata.Concat(currentMetadata).ToList();
            stopwatch.Stop();

            var initDuration = stopwatch.ElapsedMilliseconds;
            var curId = 0;
            Process? traceProcess = default;
            foreach (var rawMetadata in currentMetadata)
            {
                stopwatch.Start();
                var semanticModel = compilation.GetSemanticModel(rawMetadata.SetupNode.SyntaxTree);
                var metadata = CreateMetadata(rawMetadata, allMetadata);
                _context.Prepare(curId++, compilation, cancellationToken, metadata);

                if (cancellationToken.IsCancellationRequested)
                {
                    _log.Trace(() => new[] { "Canceled" });
                    yield break;
                }

                if (_settings.Debug)
                {
                    if (!Debugger.IsAttached)
                    {
                        _log.Info(() => new[] { "Launch a debugger." });
                        Debugger.Launch();
                    }
                    else
                    {
                        _log.Info(() => new[] { "A debugger is already attached" });
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
                var source = new Source($"{metadata.ComposerTypeName}.cs", SourceText.From(compilationUnitSyntax.ToFullString(), Encoding.UTF8));
                if (_settings.TryGetOutputPath(out outputPath))
                {
                    _log.Info(() => new[] { $"The output path: {outputPath}" });
                    _fileSystem.WriteFile(Path.Combine(outputPath, source.HintName), source.Code.ToString());
                }

                stopwatch.Stop();
                _log.Info(() => new[]
                {
                    $"Initialize {initDuration} ms.",
                    $"Generate {stopwatch.ElapsedMilliseconds} ms."
                });

                yield return source;
            }
        }

        private SourceSet GetSourceSet(Compilation compilation)
        {
            if (compilation is not CSharpCompilation csharpCompilation)
            {
                var error = $"{compilation.Language} is not supported.";
                _diagnostic.Error(Diagnostics.Error.Unsupported, error);
                throw new HandledException(error);
            }
            
            SourceSet sourceSet;
            if (_stateCache.TryGetValue(csharpCompilation.LanguageVersion, out var state))
            {
                sourceSet = state.SourceSet;
            }
            else
            {
                sourceSet = new SourceSet(new CSharpParseOptions(csharpCompilation.LanguageVersion));
                _stateCache.Add(csharpCompilation.LanguageVersion, new SourceBuilderState(sourceSet));
            }

            return sourceSet;
        }

        private IEnumerable<ResolverMetadata> GetMetadata(Compilation compilation, IEnumerable<SyntaxTree> trees, CancellationToken cancellationToken)
        {
            foreach (var tree in trees)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _log.Trace(() => new[] { "Canceled" });
                    yield break;
                }
                
                var walker = _metadataWalkerFactory(compilation.GetSemanticModel(tree));
                walker.Visit(tree.GetRoot());
                foreach (var metadata in walker.Metadata)
                {
                    yield return metadata;
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

        private IEnumerable<Source> GetComponents(Compilation compilation) =>
            AreComponentsAvailable(compilation) ? Enumerable.Empty<Source>() : GetSourceSet(compilation).ComponentSources;

        private static bool AreComponentsAvailable(Compilation compilation)
        {
            var diType = compilation.GetTypesByMetadataName(typeof(DI).FullName).FirstOrDefault();
            if (diType == null)
            {
                return false;
            }

            var type = ( 
                from tree in compilation.SyntaxTrees
                let semanticModel = compilation.GetSemanticModel(tree)
                from typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
                let symbol = semanticModel.GetDeclaredSymbol(typeDeclaration)
                select symbol).FirstOrDefault();
            
            return type != null && compilation.IsSymbolAccessibleWithin(diType, type);
        }
        
        internal class SourceBuilderState
        {
            public readonly SourceSet SourceSet;
            
            public SourceBuilderState(SourceSet sourceSet)
            {
                SourceSet = sourceSet;
            }
        }
    }
}
