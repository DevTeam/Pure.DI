// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class MetadataBuilder : IMetadataBuilder
{
    private readonly ILog<MetadataBuilder> _log;
    private readonly IDiagnostic _diagnostic;
    private readonly Func<SemanticModel, IMetadataWalker> _metadataWalkerFactory;
    private readonly ICache<LanguageVersion, SourceBuilderState> _stateCache;

    public MetadataBuilder(
        ILog<MetadataBuilder> log,
        IDiagnostic diagnostic,
        Func<SemanticModel, IMetadataWalker> metadataWalkerFactory,
        [Tag(Tags.GlobalScope)] ICache<LanguageVersion, SourceBuilderState> stateCache)
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
        var api = GetApi(compilation);
        stopwatch.Stop();

        return new MetadataContext(compilation, cancellationToken, api, featuresMetadata.AsReadOnly(), currentMetadata.AsReadOnly(), stopwatch.ElapsedMilliseconds);
    }

    private IEnumerable<Source> GetApi(Compilation compilation) =>
        AreApiAvailable(compilation) ? Enumerable.Empty<Source>() : GetSourceSet(compilation).ApiSources;

    private static bool AreApiAvailable(Compilation compilation)
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
            let symbol = ModelExtensions.GetDeclaredSymbol(semanticModel, typeDeclaration)
            select symbol).FirstOrDefault();

        return type != null && compilation.IsSymbolAccessibleWithin(diType, type);
    }

    private SourceSet GetSourceSet(Compilation compilation)
    {
        if (compilation is CSharpCompilation csharpCompilation)
        {
            return _stateCache.GetOrAdd(
                    csharpCompilation.LanguageVersion,
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
}