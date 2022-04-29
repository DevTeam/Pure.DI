// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal class MetadataBuilder : IMetadataBuilder
{
    private readonly ILog<MetadataBuilder> _log;
    private readonly IDiagnostic _diagnostic;
    private readonly Func<SemanticModel, IMetadataWalker> _metadataWalkerFactory;
    private readonly ICache<SourceSetKey, SourceBuilderState> _stateCache;

    public MetadataBuilder(
        ILog<MetadataBuilder> log,
        IDiagnostic diagnostic,
        Func<SemanticModel, IMetadataWalker> metadataWalkerFactory,
        [Tag(Tags.GlobalScope)] ICache<SourceSetKey, SourceBuilderState> stateCache)
    {
        _log = log;
        _diagnostic = diagnostic;
        _metadataWalkerFactory = metadataWalkerFactory;
        _stateCache = stateCache;
    }

    public MetadataContext Build(IExecutionContext executionContext)
    {
        var compilation = executionContext.Compilation;
        var cancellationToken = executionContext.CancellationToken;
        Stopwatch stopwatch = new();
        stopwatch.Start();
        var sourceSet = GetSourceSet(compilation);
        var syntaxTreesCount = compilation.SyntaxTrees.Count();
        compilation = compilation.AddSyntaxTrees(sourceSet.ApiTrees.Concat(sourceSet.FeaturesTrees));
        var featuresTrees = compilation.SyntaxTrees.Skip(syntaxTreesCount);
        var featuresMetadata = GetMetadata(compilation, featuresTrees, cancellationToken).ToList();
        foreach (var metadata in featuresMetadata)
        {
            metadata.DependsOn.Clear();
        }

        var currentTrees = compilation.SyntaxTrees.Take(syntaxTreesCount);
        var currentMetadata = GetMetadata(compilation, currentTrees, cancellationToken).ToList();
        stopwatch.Stop();

        return new MetadataContext(compilation, cancellationToken, GetSourceSet(compilation).ApiSources, featuresMetadata.AsReadOnly(), currentMetadata.AsReadOnly(), stopwatch.ElapsedMilliseconds);
    }

    private SourceSet GetSourceSet(Compilation compilation)
    {
        if (compilation is CSharpCompilation csharpCompilation)
        {
            return _stateCache.GetOrAdd(
                    new SourceSetKey(csharpCompilation.LanguageVersion, Defaults.DefaultNamespace),
                    _ => new SourceBuilderState(new SourceSet(new CSharpParseOptions(csharpCompilation.LanguageVersion))))
                .SourceSet;
        }

        var error = $"{compilation.Language} is not supported.";
        _diagnostic.Error(Diagnostics.Error.Unsupported, error);
        throw new HandledException(error);
    }

    private IEnumerable<ResolverMetadata> GetMetadata(Compilation compilation, IEnumerable<SyntaxTree> trees, CancellationToken cancellationToken)
    {
        foreach (var tree in trees)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _log.Trace(() => new[]
                {
                    "Canceled"
                });

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

    internal class SourceBuilderState
    {
        public readonly SourceSet SourceSet;

        public SourceBuilderState(SourceSet sourceSet)
        {
            SourceSet = sourceSet;
        }
    }
    
    internal class SourceSetKey
    {
        private readonly LanguageVersion _languageVersion;
        private readonly string _ns;

        public SourceSetKey(LanguageVersion languageVersion, string ns)
        {
            _languageVersion = languageVersion;
            _ns = ns;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            var other = (SourceSetKey)obj;
            return _languageVersion == other._languageVersion && _ns == other._ns;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)_languageVersion * 397) ^ _ns.GetHashCode();
            }
        }
    }
}