// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class BuildTools(
    IFilter filter,
    ITypeResolver typeResolver,
    IBaseSymbolsProvider baseSymbolsProvider)
    : IBuildTools
{
    public void AddPureHeader(LinesBuilder code)
    {
        code.AppendLine("#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET");
        code.AppendLine($"[{Names.SystemNamespace}Diagnostics.Contracts.Pure]");
        code.AppendLine("#endif");
    }

    public string GetDeclaration(Variable variable) =>
        variable.IsDeclared ? "" : $"{typeResolver.Resolve(variable.InstanceType)} ";
    
    public string OnInjected(BuildContext ctx, Variable variable)
    {
        if (!ctx.DependencyGraph.Source.Hints.IsOnDependencyInjectionEnabled)
        {
            return variable.VariableCode;
        }

        if (!filter.IsMeetRegularExpression(
                ctx.DependencyGraph.Source,
                (Hint.OnDependencyInjectionImplementationTypeNameRegularExpression, typeResolver.Resolve(variable.InstanceType).Name),
                (Hint.OnDependencyInjectionContractTypeNameRegularExpression, variable.ContractType.ToString()),
                (Hint.OnDependencyInjectionTagRegularExpression, variable.Injection.Tag.ValueToString()),
                (Hint.OnDependencyInjectionLifetimeRegularExpression, variable.Node.Lifetime.ValueToString())))
        {
            return variable.VariableCode;
        }
        
        var tag = GetTag(ctx, variable);
        return $"{Names.OnDependencyInjectionMethodName}<{variable.ContractType}>({variable.VariableCode}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()})";
    }
    
    public IEnumerable<Line> OnCreated(BuildContext ctx, Variable variable)
    {
        if (variable.Node.Arg is not null)
        {
            return Array.Empty<Line>();
        }

        var baseTypes = 
            baseSymbolsProvider.GetBaseSymbols(variable.InstanceType)
                .Concat(Enumerable.Repeat(variable.InstanceType, 1))
                .ToImmutableHashSet(SymbolEqualityComparer.Default);

        var lines = ctx.Accumulators
            .Where(i => baseTypes.Contains(i.Type))
            .Select(i => new Line(0, $"{i.Name}.Add({variable.VariableName});"))
            .ToList();

        if (!ctx.DependencyGraph.Source.Hints.IsOnNewInstanceEnabled)
        {
            return lines;
        }

        if (!filter.IsMeetRegularExpression(
                ctx.DependencyGraph.Source,
                (Hint.OnNewInstanceImplementationTypeNameRegularExpression, variable.Node.Type.ToString()),
                (Hint.OnNewInstanceTagRegularExpression, variable.Injection.Tag.ValueToString()),
                (Hint.OnNewInstanceLifetimeRegularExpression, variable.Node.Lifetime.ValueToString())))
        {
            return lines;
        }

        var tag = GetTag(ctx, variable);
        lines.Insert(0, new Line(0, $"{Names.OnNewInstanceMethodName}<{typeResolver.Resolve(variable.InstanceType)}>(ref {variable.VariableName}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()});"));
        return lines;
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