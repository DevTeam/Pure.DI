// ReSharper disable InvertIf
namespace Pure.DI.Core;

using NS35EBD81B;

internal sealed class BindingMetadata : IBindingMetadata
{
    private static int _currentId;
    private readonly ISet<SemanticType> _dependencies = new HashSet<SemanticType>(SemanticTypeEqualityComparer.Default);
    private readonly IDictionary<SemanticType, ISet<ExpressionSyntax>> _dependencyTags = new Dictionary<SemanticType, ISet<ExpressionSyntax>>(SemanticTypeEqualityComparer.Default);
    private readonly ISet<ExpressionSyntax> _tags = new HashSet<ExpressionSyntax>();
    private ImmutableArray<Location> _locations = ImmutableArray<Location>.Empty;

    public BindingMetadata(object? id = null)
    {
        Id = id ?? Interlocked.Increment(ref _currentId);
    }

    public BindingMetadata(IBindingMetadata binding, SemanticType dependency, object? id)
        : this(id)
    {
        if (id == null)
        {
            Id = binding.Id;
        }

        _dependencies.Add(dependency);
        Implementation = binding.Implementation;
        Factory = binding.Factory;
        Locations = binding.Locations;
        foreach (var bindingDependency in binding.Dependencies)
        {
            AddDependencyTags(bindingDependency, binding.GetTags(bindingDependency).ToArray());
        }
    }

    public object Id { get; }

    public ImmutableArray<Location> Locations
    {
        get => _locations;
        set => _locations = value.IsDefaultOrEmpty ? ImmutableArray<Location>.Empty : value;
    }

    public SemanticType? Implementation { get; set; }

    public SimpleLambdaExpressionSyntax? Factory { get; set; }

    public Lifetime Lifetime { get; set; } = Lifetime.Transient;

    public bool AnyTag { get; set; }

    public BindingType BindingType { get; set; }

    public IEnumerable<SemanticType> Dependencies => _dependencies;

    public void AddDependency(SemanticType dependency) => _dependencies.Add(dependency);

    public IEnumerable<ExpressionSyntax> GetTags(SemanticType dependencyType)
    {
        var tags = new HashSet<ExpressionSyntax>(_tags);
        if (_dependencyTags.TryGetValue(dependencyType, out var dependencyTags))
        {
            foreach (var dependencyTag in dependencyTags)
            {
                tags.Add(dependencyTag);
            }
        }

        return tags;
    }

    public void AddTags(params ExpressionSyntax[] tags)
    {
        foreach (var tag in tags)
        {
            _tags.Add(tag);
        }
    }
    
    public void AddDependencyTags(SemanticType dependency, params ExpressionSyntax[] tags)
    {
        if (!_dependencyTags.TryGetValue(dependency, out var curTags))
        {
            curTags = new HashSet<ExpressionSyntax>();
            _dependencyTags.Add(dependency, curTags);
        }

        foreach (var tag in tags)
        {
            curTags.Add(tag);
        }
    }

    [SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
    public override string ToString()
    {
        StringBuilder sb = new();
        switch (BindingType)
        {
            case BindingType.Arg:
                AppendMethod(sb, nameof(IConfiguration.Arg));
                break;

            case BindingType.Root:
                AppendMethod(sb, nameof(IConfiguration.Root));
                break;
            
            default:
                AppendMethod(sb, nameof(IConfiguration.Bind));
                AppendLifetime(sb);
                AppendTags(sb);
                AppendTo(sb);
                break;
        }
        
        return sb.ToString();
    }

    private void AppendMethod(StringBuilder sb, string method)
    {
        foreach (var dependency in _dependencies)
        {
            var tags = string.Empty;
            if (_dependencyTags.TryGetValue(dependency, out var dependencyTags))
            {
                tags = string.Join(", ", dependencyTags);
            }

            sb.Append($"{method}<{dependency}>({tags}).");
        }
    }

    private void AppendTo(StringBuilder sb)
    {
        sb.Append(Factory != null ? $"{nameof(IBinding.To)}({Factory.ToString()})" : $"{nameof(IBinding.To)}<{Implementation}>()");
    }

    private void AppendTags(StringBuilder sb)
    {
        foreach (var tag in _tags)
        {
            sb.Append($"{nameof(IBinding.Tags)}<{tag.ToString()}>().");
        }
    }

    private void AppendLifetime(StringBuilder sb)
    {
        if (Lifetime != Lifetime.Transient)
        {
            sb.Append($"{nameof(IBinding.As)}({Lifetime}).");
        }
    }
}