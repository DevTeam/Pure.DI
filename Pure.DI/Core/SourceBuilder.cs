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
    using System.Text.RegularExpressions;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SourceBuilder : ISourceBuilder
    {
        private static readonly Regex FeaturesRegex = new(@"Pure.DI.Features.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex ComponentsRegex = new(@"Pure.DI.Components.[\w]+.cs", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly List<Source> Features;
        private static readonly List<Source> Components;
        private static readonly List<Source> ComponentsInUniqNamespace;
        private readonly IBuildContext _context;
        private readonly ISettings _settings;
        private readonly IFileSystem _fileSystem;
        private readonly IDiagnostic _diagnostic;
        private readonly ILog<SourceBuilder> _log;
        private readonly Func<IClassBuilder> _resolverBuilderFactory;
        private readonly Func<SemanticModel, IMetadataWalker> _metadataWalkerFactory;

        static SourceBuilder()
        {
            Features = new List<Source> { new("Features.cs",SourceText.From(string.Join(Environment.NewLine, GetResources(FeaturesRegex).Select(i => i.code)), Encoding.UTF8)) };
            Components = GetResources(ComponentsRegex).Select(i => new Source(i.file, SourceText.From(i.code, Encoding.UTF8))).ToList();
            // ReSharper disable once ReplaceSubstringWithRangeIndexer
            var id = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 4);
            ComponentsInUniqNamespace = GetResources(ComponentsRegex).Select(i => new Source(i.file, SourceText.From(i.code.Replace("Pure.DI", "Pure.DI." + id), Encoding.UTF8))).ToList();
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

            var syntaxTreesCount = compilation.SyntaxTrees.Count();
            var componentsInUniqNamespaceIsNeeded = ComponentsInUniqNamespaceIsNeeded(compilation);
            var components = new List<Source>();
            components.AddRange(componentsInUniqNamespaceIsNeeded ? ComponentsInUniqNamespace : Components);

            var parseOptions = new CSharpParseOptions(csharpCompilation.LanguageVersion);
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var component in components)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                
                compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(component.Code, parseOptions));
                if (!componentsInUniqNamespaceIsNeeded)
                {
                    yield return component;
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var feature in Features)
                {
                    compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(feature.Code, parseOptions));                    
                }

                var featuresMetadata = new List<ResolverMetadata>();
                foreach (var tree in compilation.SyntaxTrees.Skip(syntaxTreesCount))
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
                    foreach (var tree in compilation.SyntaxTrees.Take(syntaxTreesCount))
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
                        var curId = 0;
                        foreach (var (tree, rawMetadata) in items)
                        {
                            stopwatch.Start();
                            var semanticModel = compilation.GetSemanticModel(tree);
                            var metadata = CreateMetadata(rawMetadata, allMetadata);
                            _context.Prepare(curId++, compilation, cancellationToken, metadata);

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
                            
                            if (_settings.Trace && _settings.TryGetOutputPath(out var outputPath))
                            {
                                var traceFile = Path.Combine(outputPath, $"dotTrace{Guid.NewGuid().ToString().Substring(0, 4)}.dtt");
                                var profiler = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools", "dottrace.exe");
                                new Process {
                                    StartInfo = new ProcessStartInfo(
                                        profiler, 
                                        $@"attach {Process.GetCurrentProcess().Id} --save-to=""{traceFile}"" --profiling-type=Sampling")
                                    {
                                        WindowStyle = ProcessWindowStyle.Hidden,
                                        CreateNoWindow = true
                                    }
                                }.Start();
                                Thread.Sleep(1000);
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

                            var source = new Source(
                                $"{metadata.ComposerTypeName}.cs",
                                SourceText.From(compilationUnitSyntax.ToString(), Encoding.UTF8));

                            if (_settings.TryGetOutputPath(out outputPath))
                            {
                                _log.Info(() => new[] {$"The output path: {outputPath}"});
                                _fileSystem.WriteFile(Path.Combine(outputPath, source.HintName), source.Code.ToString());
                                foreach (var feature in Features)
                                {
                                    _fileSystem.WriteFile(Path.Combine(outputPath, feature.HintName), feature.Code.ToString());                                    
                                }

                                foreach (var component in components)
                                {
                                    _fileSystem.WriteFile(Path.Combine(outputPath, component.HintName), component.Code.ToString());
                                }
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

        private static bool ComponentsInUniqNamespaceIsNeeded(Compilation compilation)
        {
            var diType = compilation.GetTypeByMetadataName(typeof(IConfiguration).FullName);
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
    }
}
