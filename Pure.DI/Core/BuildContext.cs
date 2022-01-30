namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal class BuildContext : IBuildContext
{
    private readonly Dictionary<MemberKey, MemberDeclarationSyntax> _additionalMembers = new();
    private readonly HashSet<IBindingMetadata> _additionalBindings = new();
    private readonly HashSet<StatementSyntax> _finalizationStatements = new();
    private readonly HashSet<StatementSyntax> _finalDisposeStatements = new();
    private readonly Func<INameService> _nameServiceFactory;
    private readonly Func<ITypeResolver> _typeResolverFactory;
    private Compilation? _compilation;
    private CancellationToken? _cancellationToken;
    private ResolverMetadata? _metadata;
    private INameService? _nameService;
    private ITypeResolver? _typeResolver;

    public BuildContext(
        [Tag(Tags.Default)] Func<INameService> nameServiceFactory,
        [Tag(Tags.Default)] Func<ITypeResolver> typeResolverFactory)
    {
        _nameServiceFactory = nameServiceFactory;
        _typeResolverFactory = typeResolverFactory;
    }

    public int Id { get; private set; }

    public Compilation Compilation => _compilation ?? throw new InvalidOperationException("Not initialized.");

    public bool IsCancellationRequested => _cancellationToken is { IsCancellationRequested: true };

    public ResolverMetadata Metadata => _metadata ?? throw new InvalidOperationException("Not initialized.");

    public INameService NameService => _nameService ?? throw new InvalidOperationException("Not ready.");

    public ITypeResolver TypeResolver => _typeResolver ?? throw new InvalidOperationException("Not ready.");

    public IEnumerable<IBindingMetadata> AdditionalBindings => _additionalBindings;

    public IEnumerable<MemberDeclarationSyntax> AdditionalMembers => _additionalMembers.Values;

    public IEnumerable<StatementSyntax> FinalizationStatements => _finalizationStatements;

    public IEnumerable<StatementSyntax> FinalDisposeStatements => _finalDisposeStatements;

    public void Prepare(int id, Compilation compilation, CancellationToken cancellationToken, ResolverMetadata metadata)
    {
        Id = id;
        _compilation = compilation;
        _cancellationToken = cancellationToken;
        _metadata = metadata;
        _additionalBindings.Clear();
        _nameService = _nameServiceFactory();
        _typeResolver = _typeResolverFactory();
        _additionalMembers.Clear();
        _finalizationStatements.Clear();
        _finalDisposeStatements.Clear();
    }

    public void AddBinding(IBindingMetadata binding) => _additionalBindings.Add(binding);

    public T GetOrAddMember<T>(MemberKey key, Func<T> additionalMemberFactory)
        where T : MemberDeclarationSyntax
    {
        if (_additionalMembers.TryGetValue(key, out var member))
        {
            return (T)member;
        }

        member = additionalMemberFactory();
        _additionalMembers.Add(key, member);
        return (T)member;
    }

    public void AddFinalizationStatements(IEnumerable<StatementSyntax> finalizationStatements)
    {
        foreach (var finalizationStatement in finalizationStatements)
        {
            _finalizationStatements.Add(finalizationStatement);
        }
    }

    public void AddFinalDisposeStatements(IEnumerable<StatementSyntax> releaseStatements)
    {
        foreach (var releaseStatement in releaseStatements)
        {
            _finalDisposeStatements.Add(releaseStatement);
        }
    }
}