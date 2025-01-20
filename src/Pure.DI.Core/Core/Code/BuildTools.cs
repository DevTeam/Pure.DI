// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

using static Tag;

internal class BuildTools(
    IFilter filter,
    ITypeResolver typeResolver,
    IBaseSymbolsProvider baseSymbolsProvider,
    [Tag(Injection)] IIdGenerator idGenerator,
    ILocks locks,
    ISymbolNames symbolNames)
    : IBuildTools
{
    public void AddPureHeader(LinesBuilder code)
    {
        code.AppendLine("#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET");
        code.AppendLine($"[{Names.SystemNamespace}Diagnostics.Contracts.Pure]");
        code.AppendLine("#endif");
    }

    public string GetDeclaration(Variable variable, string separator = " ") =>
        variable.IsDeclared ? "" : $"{typeResolver.Resolve(variable.Setup, variable.InstanceType)}{separator}";

    public string OnInjected(BuildContext ctx, Variable variable)
    {
        var injection = OnInjectedInternal(ctx, variable);
        var refKind = variable.RefKind switch
        {
            RefKind.Ref
#if ROSLYN4_8_OR_GREATER
                or RefKind.RefReadOnlyParameter
#endif
                => "ref",
            RefKind.Out => "out",
            _ => ""
        };

        if (!string.IsNullOrEmpty(refKind))
        {
            var localVarName = $"{variable.VariableDeclarationName}_{refKind}{idGenerator.Generate()}";
            ctx.Code.AppendLine($"{variable.InstanceType} {localVarName} = {injection};");
            injection = $"{refKind} {localVarName}";
        }

        return injection;
    }

    private string OnInjectedInternal(BuildContext ctx, Variable variable)
    {
        var variableCode = variable.VariableCode;
        if (variableCode == variable.VariableName)
        {
            var skipNotNullCheck =
                variable.InstanceType.IsReferenceType
                && ctx.DependencyGraph.Source.SemanticModel.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable
                && (variable.HasCycle || variable.Node.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve);

            if (skipNotNullCheck && (variable.HasCycle || variable.Node.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve))
            {
                variableCode = $"{variableCode}";
            }
        }

        if (!ctx.DependencyGraph.Source.Hints.IsOnDependencyInjectionEnabled)
        {
            return variableCode;
        }

        var tag = GetTag(ctx, variable);
        var typeName = new Lazy<string>(() => typeResolver.Resolve(variable.Setup, variable.InstanceType).Name);
        var contractName = new Lazy<string>(() => symbolNames.GetName(variable.ContractType));
        var tagName = new Lazy<string>(() => tag.ValueToString());
        var lifetimeName = new Lazy<string>(() => variable.Node.Lifetime.ValueToString());
        if (!filter.IsMeetRegularExpressions(
                ctx.DependencyGraph.Source,
                (Hint.OnDependencyInjectionImplementationTypeNameRegularExpression, typeName),
                (Hint.OnDependencyInjectionContractTypeNameRegularExpression, contractName),
                (Hint.OnDependencyInjectionTagRegularExpression, tagName),
                (Hint.OnDependencyInjectionLifetimeRegularExpression, lifetimeName))
            || !filter.IsMeetWildcards(
                ctx.DependencyGraph.Source,
                (Hint.OnDependencyInjectionImplementationTypeNameWildcard, typeName),
                (Hint.OnDependencyInjectionContractTypeNameWildcard, contractName),
                (Hint.OnDependencyInjectionTagWildcard, tagName),
                (Hint.OnDependencyInjectionLifetimeWildcard, lifetimeName)))
        {
            return variableCode;
        }
        
        return $"{Names.OnDependencyInjectionMethodName}<{typeResolver.Resolve(variable.Setup, variable.ContractType)}>({variableCode}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()})";
    }

    public IEnumerable<Line> OnCreated(BuildContext ctx, Variable variable)
    {
        if (variable.Node.Arg is not null)
        {
            return Array.Empty<Line>();
        }

        var baseTypes = new Lazy<ImmutableHashSet<ISymbol?>>(() =>
            baseSymbolsProvider.GetBaseSymbols(variable.InstanceType, (_, _) => true)
                .ToImmutableHashSet(SymbolEqualityComparer.Default));

        var code = new LinesBuilder();
        var lockIsRequired = ctx.LockIsRequired ?? ctx.DependencyGraph.Source.Hints.IsThreadSafeEnabled;
        var accLines = ctx.Accumulators
            .Where(i => FilterAccumulator(i, variable.Node.Lifetime))
            .Where(i => baseTypes.Value.Contains(i.Type))
            .GroupBy(i => i.Name)
            .Select(i => i.First())
            .OrderBy(i => i.Name)
            .Select(i => new Line(0, $"{i.Name}.Add({variable.VariableName});"))
            .ToList();

        if (lockIsRequired && accLines.Count > 0)
        {
            locks.AddLockStatements(ctx.DependencyGraph.Source, code, false);
            code.AppendLine("{");
            code.IncIndent();
        }

        code.AppendLines(accLines);

        if (lockIsRequired && accLines.Count > 0)
        {
            code.DecIndent();
            code.AppendLine("}");
            locks.AddUnlockStatements(ctx.DependencyGraph.Source, code, false);
        }

        if (!ctx.DependencyGraph.Source.Hints.IsOnNewInstanceEnabled)
        {
            return code.Lines;
        }

        var tag = GetTag(ctx, variable);
        var typeName = new Lazy<string>(() => symbolNames.GetName(variable.Node.Type));
        var tagName = new Lazy<string>(() => tag.ValueToString());
        var lifetimeName = new Lazy<string>(() => variable.Node.Lifetime.ValueToString());
        if (!filter.IsMeetRegularExpressions(
                ctx.DependencyGraph.Source,
                (Hint.OnNewInstanceImplementationTypeNameRegularExpression, typeName),
                (Hint.OnNewInstanceTagRegularExpression, tagName),
                (Hint.OnNewInstanceLifetimeRegularExpression, lifetimeName))
            || !filter.IsMeetWildcards(
                ctx.DependencyGraph.Source,
                (Hint.OnNewInstanceImplementationTypeNameWildcard, typeName),
                (Hint.OnNewInstanceTagWildcard, tagName),
                (Hint.OnNewInstanceLifetimeWildcard, lifetimeName)))
        {
            return code.Lines;
        }
        
        var lines = new List<Line>
        {
            new(0, $"{Names.OnNewInstanceMethodName}<{typeResolver.Resolve(variable.Setup, variable.InstanceType)}>(ref {variable.VariableName}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()});")
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