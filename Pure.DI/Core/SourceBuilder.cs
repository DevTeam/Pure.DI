namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Text;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SourceBuilder : ISourceBuilder
    {
        private static readonly Regex FeaturesRegex = new(@"Pure.DI.Features.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex ComponentsRegex = new(@"Pure.DI.Components.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Source Features;
        private static readonly IEnumerable<Source> Components;
        private readonly IBuildContext _context;
        private readonly ISettings _settings;
        private readonly IFileSystem _fileSystem;
        private readonly IDiagnostic _diagnostic;
        private readonly ILog<SourceBuilder> _log;
        private readonly Func<IClassBuilder> _resolverBuilderFactory;
        private readonly Func<SemanticModel, IMetadataWalker> _metadataWalkerFactory;

        static SourceBuilder()
        {
            Features = new Source("Features.cs",SourceText.From(string.Join(Environment.NewLine, GetResources(FeaturesRegex).Select(i => i.code)), Encoding.UTF8));
            Components = GetResources(ComponentsRegex).Select(i => new Source(i.file, SourceText.From(i.code, Encoding.UTF8))).ToArray();
        }

        public SourceBuilder(
            IBuildContext context,
            ISettings settings,
            IFileSystem fileSystem,
            IDiagnostic diagnostic,
            ILog<SourceBuilder> log,
            Func<IClassBuilder> resolverBuilderFactory,
            Func<SemanticModel, IMetadataWalker> metadataWalkerFactory)
        {
            _context = context;
            _settings = settings;
            _fileSystem = fileSystem;
            _diagnostic = diagnostic;
            _log = log;
            _resolverBuilderFactory = resolverBuilderFactory;
            _metadataWalkerFactory = metadataWalkerFactory;
        }

        public IEnumerable<Source> Build(Compilation compilation, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            if (compilation is not CSharpCompilation csharpCompilation)
            {
                var error = $"{compilation.Language} is not supported.";
                _diagnostic.Error(Diagnostics.Error.Unsupported, error);
                throw new HandledException(error);
            }

            var parseOptions = new CSharpParseOptions(csharpCompilation.LanguageVersion);
            foreach (var component in Components)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(component.Code, parseOptions));
                yield return component;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(Features.Code, parseOptions));

                var featuresMetadata = new List<ResolverMetadata>();
                foreach (var tree in compilation.SyntaxTrees.Reverse().Take(1))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var walker = _metadataWalkerFactory(compilation.GetSemanticModel(tree));
                    walker.Visit(tree.GetRoot());
                    featuresMetadata.AddRange(walker.Metadata);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    var allMetadata = new List<ResolverMetadata>(featuresMetadata);
                    List<(SyntaxTree tree, ResolverMetadata rawMetadata)> items = new();
                    foreach (var tree in compilation.SyntaxTrees.Reverse().Skip(1 + Components.Count()))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        var walker = _metadataWalkerFactory(compilation.GetSemanticModel(tree));
                        walker.Visit(tree.GetRoot());
                        allMetadata.AddRange(walker.Metadata);
                        items.AddRange(walker.Metadata.Select(rawMetadata => (tree, rawMetadata)));
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        stopwatch.Stop();
                        var initDuration = stopwatch.ElapsedMilliseconds;

                        foreach (var (tree, rawMetadata) in items)
                        {
                            stopwatch.Start();
                            var semanticModel = compilation.GetSemanticModel(tree);
                            var metadata = CreateMetadata(rawMetadata, allMetadata);
                            _context.Prepare(compilation, cancellationToken, metadata);

                            if (cancellationToken.IsCancellationRequested)
                            {
                                _log.Trace(() => new[] { "Build canceled" });
                                break;
                            }

                            if (_settings.Debug)
                            {
                                if (!Debugger.IsAttached)
                                {
                                    _log.Info(() => new[] {"Launch a debugger."});
                                    Debugger.Launch();
                                }
                                else
                                {
                                    _log.Info(() => new[] {"A debugger is already attached"});
                                }
                            }

                            _log.Info(() =>
                            {
                                var messages = new List<string>
                                {
                                    $"Processing {rawMetadata.TargetTypeName}",
                                    $"{nameof(DI)}.{nameof(DI.Setup)}()"
                                };
                                messages.AddRange(metadata.Bindings.Select(binding => $"  .{binding}"));
                                return messages.ToArray();
                            });

                            var compilationUnitSyntax = _resolverBuilderFactory().Build(semanticModel).NormalizeWhitespace();

                            var source = new Source(
                                $"{metadata.TargetTypeName}.cs",
                                SourceText.From(compilationUnitSyntax.ToString(), Encoding.UTF8));

                            if (_settings.TryGetOutputPath(out var outputPath))
                            {
                                _log.Info(() => new[] {$"The output path: {outputPath}"});
                                _fileSystem.WriteFile(Path.Combine(outputPath, source.HintName), source.Code.ToString());
                                _fileSystem.WriteFile(Path.Combine(outputPath, Features.HintName), Features.Code.ToString());
                                foreach (var component in Components)
                                {
                                    _fileSystem.WriteFile(Path.Combine(outputPath, component.HintName), component.Code.ToString());
                                }
                            }

                            stopwatch.Stop();
                            _log.Trace(() => new[]
                            {
                                $"Initialize {initDuration} ms.",
                                $"Generate {stopwatch.ElapsedMilliseconds} ms."
                            });

                            yield return source;
                        }
                    }
                }
            }
        }

        private static ResolverMetadata CreateMetadata(ResolverMetadata metadata, IReadOnlyCollection<ResolverMetadata> allMetadata)
        {
            var newMetadata = new ResolverMetadata(metadata.SetupNode, metadata.TargetTypeName, metadata.Owner);
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
                where dependencyName.Equals(dependency.TargetTypeName, StringComparison.InvariantCultureIgnoreCase)
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

        private static IEnumerable<(string file, string code)> GetResources(Regex filter)
        {
            var assembly = typeof(SourceGenerator).Assembly;

            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!filter.IsMatch(resourceName))
                {
                    continue;
                }

                using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Cannot read {resourceName}."));
                var code = reader.ReadToEnd();
                yield return (resourceName, code);
            }
        }
    }
}
