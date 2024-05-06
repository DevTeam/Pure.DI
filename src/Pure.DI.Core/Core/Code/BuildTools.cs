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
        var variableCode = variable.VariableCode;
        if (variableCode == variable.VariableName)
        {
            var skipNotNullCheck = (variable.HasCycle || variable.Info.HasCode)
                && variable.InstanceType.IsReferenceType
                && ctx.DependencyGraph.Source.SemanticModel.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable;

            if (skipNotNullCheck)
            {
                variableCode = variable.Node.Lifetime switch
                {
                    Lifetime.Singleton or Lifetime.PerResolve or Lifetime.Scoped => $"{variableCode}!",
                    _ => variableCode
                };
            }
        }

        if (!ctx.DependencyGraph.Source.Hints.IsOnDependencyInjectionEnabled)
        {
            return variableCode;
        }

        if (!filter.IsMeetRegularExpression(
                ctx.DependencyGraph.Source,
                (Hint.OnDependencyInjectionImplementationTypeNameRegularExpression, typeResolver.Resolve(variable.InstanceType).Name),
                (Hint.OnDependencyInjectionContractTypeNameRegularExpression, variable.ContractType.ToString()),
                (Hint.OnDependencyInjectionTagRegularExpression, variable.Injection.Tag.ValueToString()),
                (Hint.OnDependencyInjectionLifetimeRegularExpression, variable.Node.Lifetime.ValueToString())))
        {
            return variableCode;
        }
        
        var tag = GetTag(ctx, variable);
        return $"{Names.OnDependencyInjectionMethodName}<{variable.ContractType}>({variableCode}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()})";
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

        var code = new LinesBuilder();
        var lockIsRequired = ctx.LockIsRequired ?? ctx.DependencyGraph.Source.Hints.IsThreadSafeEnabled;
        var accLines = ctx.Accumulators
            .Where(i => FilterAccumulator(i, variable.Node.Lifetime))
            .Where(i => baseTypes.Contains(i.Type))
            .GroupBy(i => i.Name)
            .Select(i => i.First())
            .OrderBy(i => i.Name)
            .Select(i => new Line(0, $"{i.Name}.Add({variable.VariableName});"))
            .ToList();
        
        if (lockIsRequired && accLines.Count > 0)
        {
            code.AppendLine($"lock ({Names.LockFieldName})");
            code.AppendLine("{");
            code.IncIndent();
        }

        code.AppendLines(accLines);

        if (lockIsRequired && accLines.Count > 0)
        {
            code.DecIndent();
            code.AppendLine("}");
        }

        if (!ctx.DependencyGraph.Source.Hints.IsOnNewInstanceEnabled)
        {
            return code.Lines;
        }

        if (!filter.IsMeetRegularExpression(
                ctx.DependencyGraph.Source,
                (Hint.OnNewInstanceImplementationTypeNameRegularExpression, variable.Node.Type.ToString()),
                (Hint.OnNewInstanceTagRegularExpression, variable.Injection.Tag.ValueToString()),
                (Hint.OnNewInstanceLifetimeRegularExpression, variable.Node.Lifetime.ValueToString())))
        {
            return code.Lines;
        }

        var tag = GetTag(ctx, variable);
        var lines = new List<Line>
        {
            new(0, $"{Names.OnNewInstanceMethodName}<{typeResolver.Resolve(variable.InstanceType)}>(ref {variable.VariableName}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()});")
        };
        
        lines.AddRange(code.Lines);
        return lines;
    }

    private static bool FilterAccumulator(Accumulator accumulator, Lifetime lifetime)
    {
        if (accumulator.Lifetime != lifetime)
        {
            return false;
        }
        
        if (accumulator.IsRoot)
        {
            return true;
        }
        
        return lifetime is not (Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve);
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