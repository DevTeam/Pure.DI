// ReSharper disable InvertIf
namespace Pure.DI.Core.Code;

internal class BuildTools : IBuildTools
{
    private readonly Dictionary<Compilation, INamedTypeSymbol?> _disposableTypes = new();
    private readonly IFilter _filter;

    public BuildTools(IFilter filter) => _filter = filter;

    public string GetDeclaration(Variable variable) => variable.IsDeclared ? "" : $"{variable.InstanceType} ";
    
    public string OnInjected(BuildContext ctx, Variable variable)
    {
        if (ctx.DependencyGraph.Source.Hints.GetHint(Hint.OnDependencyInjection) != SettingState.On)
        {
            return variable.VarName;
        }

        if (!_filter.IsMeetRegularExpression(
                ctx.DependencyGraph.Source,
                (Hint.OnDependencyInjectionImplementationTypeNameRegularExpression, variable.InstanceType.ToString()),
                (Hint.OnDependencyInjectionContractTypeNameRegularExpression, variable.ContractType.ToString()),
                (Hint.OnDependencyInjectionTagRegularExpression, variable.Injection.Tag.ValueToString()),
                (Hint.OnDependencyInjectionLifetimeRegularExpression, variable.Node.Lifetime.ValueToString())))
        {
            return variable.VarName;
        }
        
        var tag = GetTag(ctx, variable);
        return $"{Names.OnDependencyInjectionMethodName}<{variable.ContractType}>({variable.VarName}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()})";
    }
    
    public IEnumerable<Line> OnCreated(BuildContext ctx, Variable variable)
    {
        if (variable.Node.Arg is not null)
        {
            yield break;
        }

        if (ctx.DependencyGraph.Source.Hints.GetHint(Hint.OnNewInstance) != SettingState.On)
        {
            yield break;
        }

        if (!_filter.IsMeetRegularExpression(
                ctx.DependencyGraph.Source,
                (Hint.OnNewInstanceImplementationTypeNameRegularExpression, variable.Node.Type.ToString()),
                (Hint.OnNewInstanceTagRegularExpression, variable.Injection.Tag.ValueToString()),
                (Hint.OnNewInstanceLifetimeRegularExpression, variable.Node.Lifetime.ValueToString())))
        {
            yield break;
        }

        var tag = GetTag(ctx, variable);
        yield return new Line(0, $"{Names.OnNewInstanceMethodName}<{variable.InstanceType}>(ref {variable.VarName}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()})" + ";");
    }

    public bool IsDisposable(Variable variable)
    {
        var compilation = variable.Node.Binding.SemanticModel.Compilation;
        if (!_disposableTypes.TryGetValue(compilation, out var disposableType))
        {
            disposableType = compilation.GetTypeByMetadataName(Names.IDisposableInterfaceName);
            _disposableTypes.Add(compilation, disposableType);
        }
        
        return disposableType is not null && variable.Node.Type.AllInterfaces.Any(i => disposableType.Equals(i, SymbolEqualityComparer.Default));
    }

    private static object? GetTag(BuildContext ctx, Variable variable)
    {
        var tag = variable.Injection.Tag;
        if (ReferenceEquals(tag, MdTag.ContextTag))
        {
            tag = ctx.ContextTag;
        }

        return tag;
    }
}