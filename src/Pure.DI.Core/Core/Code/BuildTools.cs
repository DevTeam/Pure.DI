// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class BuildTools(IFilter filter, ITypeResolver typeResolver) : IBuildTools
{
    public void AddPureHeader(LinesBuilder code)
    {
        code.AppendLine("#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET");
        code.AppendLine($"[{Names.SystemNamespace}Diagnostics.Contracts.Pure]");
        code.AppendLine("#endif");
    }

    public string GetDeclaration(Variable variable, bool typeIsRequired = false) =>
        variable.IsDeclared ? "" : typeIsRequired ? $"{typeResolver.Resolve(variable.InstanceType)} " : "var ";
    
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

        if (!ctx.DependencyGraph.Source.Hints.IsOnNewInstanceEnabled)
        {
            return Array.Empty<Line>();
        }

        if (!filter.IsMeetRegularExpression(
                ctx.DependencyGraph.Source,
                (Hint.OnNewInstanceImplementationTypeNameRegularExpression, variable.Node.Type.ToString()),
                (Hint.OnNewInstanceTagRegularExpression, variable.Injection.Tag.ValueToString()),
                (Hint.OnNewInstanceLifetimeRegularExpression, variable.Node.Lifetime.ValueToString())))
        {
            return Array.Empty<Line>();
        }

        var tag = GetTag(ctx, variable);
        return [new Line(0, $"{Names.OnNewInstanceMethodName}<{typeResolver.Resolve(variable.InstanceType)}>(ref {variable.VariableName}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()})" + ";")];
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