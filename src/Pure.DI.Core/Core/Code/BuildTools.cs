// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable MoveLocalFunctionAfterJumpStatement
namespace Pure.DI.Core.Code;

using v2;
using static LinesBuilderExtensions;

sealed class BuildTools(
    IFilter filter,
    ITypeResolver typeResolver,
    IBaseSymbolsProvider baseSymbolsProvider,
    [Tag(Tag.Injection)] IIdGenerator idGenerator,
    ILocks locks,
    ISymbolNames symbolNames,
    ICompilations compilations)
    : IBuildTools
{
    public string NullCheck(Compilation compilation, string variableName)
    {
        var languageVersion = compilations.GetLanguageVersion(compilation);
        return languageVersion >= LanguageVersion.CSharp9
            ? $"{variableName} is null"
            : $"{Names.ObjectTypeName}.ReferenceEquals({variableName}, null)";
    }

    public void AddPureHeader(LinesBuilder code)
    {
        code.AppendLine(new Line(int.MinValue, "#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET // Pure method"));
        code.AppendLine($"[{Names.SystemNamespace}Diagnostics.Contracts.Pure]");
        code.AppendLine(new Line(int.MinValue, "#endif // Pure method"));
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

    public IEnumerable<Line> OnCreated(BuildContext ctx, Variable variable)
    {
        if (variable.Node.Arg is not null)
        {
            return [];
        }

        var baseTypes = new Lazy<ImmutableHashSet<ISymbol?>>(() =>
            baseSymbolsProvider.GetBaseSymbols(variable.InstanceType, (_, _) => true)
                .ToImmutableHashSet(SymbolEqualityComparer.Default));

        var code = new LinesBuilder();
        var lockIsRequired = ctx.LockIsRequired ?? ctx.DependencyGraph.Source.Hints.IsThreadSafeEnabled;
        var accLines = ctx.Accumulators
            .Where(i => FilterAccumulator(i, variable.Node.Lifetime))
            .Where(i => baseTypes.Value.Contains(i.Type))
            .GroupBy(i => i.Var.Name)
            .Select(i => i.First())
            .OrderBy(i => i.Var.Name)
            .Select(i => new Line(0, $"{i.Var.Name}.Add({variable.VariableName});"))
            .ToList();

        if (lockIsRequired && accLines.Count > 0)
        {
            locks.AddLockStatements(ctx.DependencyGraph, code, false);
            code.AppendLine(BlockStart);
            code.IncIndent();
        }

        code.AppendLines(accLines);

        if (lockIsRequired && accLines.Count > 0)
        {
            code.DecIndent();
            code.AppendLine(BlockFinish);
            locks.AddUnlockStatements(ctx.DependencyGraph, code, false);
        }

        if (!ctx.DependencyGraph.Source.Hints.IsOnNewInstanceEnabled)
        {
            return code.Lines;
        }

        var tag = GetTag(ctx, variable);
        string GetTypeName() => symbolNames.GetName(variable.Node.Type);
        string GetTagName() => tag.ValueToString();
        string GetLifetimeName() => variable.Node.Lifetime.ValueToString();
        if (!filter.IsMeet(
                ctx.DependencyGraph.Source,
                (Hint.OnNewInstanceImplementationTypeNameRegularExpression, Hint.OnNewInstanceImplementationTypeNameWildcard, GetTypeName),
                (Hint.OnNewInstanceTagRegularExpression, Hint.OnNewInstanceTagWildcard, GetTagName),
                (Hint.OnNewInstanceLifetimeRegularExpression, Hint.OnNewInstanceLifetimeWildcard, GetLifetimeName)))
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

    public void AddAggressiveInlining(LinesBuilder code)
    {
        code.AppendLine($"[{Names.MethodImplAttributeName}(({Names.MethodImplOptionsName})256)]");
    }

    public string GetDeclaration(CodeContext ctx, Var var, string separator = " ")
    {
        return var.Declaration.IsDeclared ? "" : $"{typeResolver.Resolve(ctx.RootContext.Graph.Source, var.InstanceType)}{separator}";
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
        string GetTypeName() => typeResolver.Resolve(variable.Setup, variable.InstanceType).Name;
        string GetContractName() => symbolNames.GetName(variable.ContractType);
        string GetTagName() => tag.ValueToString();
        string GetLifetimeName() => variable.Node.Lifetime.ValueToString();
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (!filter.IsMeet(
                ctx.DependencyGraph.Source,
                (Hint.OnDependencyInjectionImplementationTypeNameRegularExpression, Hint.OnDependencyInjectionImplementationTypeNameWildcard, GetTypeName),
                (Hint.OnDependencyInjectionContractTypeNameRegularExpression, Hint.OnDependencyInjectionContractTypeNameWildcard, GetContractName),
                (Hint.OnDependencyInjectionTagRegularExpression, Hint.OnDependencyInjectionTagWildcard, GetTagName),
                (Hint.OnDependencyInjectionLifetimeRegularExpression, Hint.OnDependencyInjectionLifetimeWildcard, GetLifetimeName)))
        {
            return variableCode;
        }

        return $"{Names.OnDependencyInjectionMethodName}<{typeResolver.Resolve(variable.Setup, variable.ContractType)}>({variableCode}, {tag.ValueToString()}, {variable.Node.Lifetime.ValueToString()})";
    }

    private static bool FilterAccumulator(Accumulator accumulator, Lifetime lifetime)
    {
        if (accumulator.Lifetime != lifetime)
        {
            return false;
        }

        /*if (accumulator.IsRoot)
        {
            return true;
        }*/

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

    public IEnumerable<Line> OnCreated(CodeContext ctx, Var var)
    {
        if (var.Node.Arg is not null)
        {
            return [];
        }

        var baseTypes = new Lazy<ImmutableHashSet<ISymbol?>>(() =>
            baseSymbolsProvider.GetBaseSymbols(var.InstanceType, (_, _) => true)
                .ToImmutableHashSet(SymbolEqualityComparer.Default));

        var accLines = ctx.Accumulators
            .Where(acc => acc.Lifetime == var.Node.Lifetime)
            .Where(acc => baseTypes.Value.Contains(acc.Type))
            .GroupBy(acc => acc.Var.Name)
            .Select(grouping => grouping.First())
            .OrderBy(i => i.Var.Name)
            .Select(i => new Line(0, $"{i.Var.Name}.Add({var.Name});"))
            .ToList();

        var code = new LinesBuilder();
        if (ctx.IsLockRequired && accLines.Count > 0)
        {
            locks.AddLockStatements(ctx.RootContext.Graph, code, false);
            code.AppendLine(BlockStart);
            code.IncIndent();
        }

        code.AppendLines(accLines);

        if (ctx.IsLockRequired && accLines.Count > 0)
        {
            code.DecIndent();
            code.AppendLine(BlockFinish);
            locks.AddUnlockStatements(ctx.RootContext.Graph, code, false);
        }

        if (!ctx.RootContext.Graph.Source.Hints.IsOnNewInstanceEnabled)
        {
            return code.Lines;
        }

        var tag = GetTag(ctx, var);
        string GetTypeName() => symbolNames.GetName(var.InstanceType);
        string GetTagName() => tag.ValueToString();
        string GetLifetimeName() => var.Node.Lifetime.ValueToString();
        if (!filter.IsMeet(
                ctx.RootContext.Graph.Source,
                (Hint.OnNewInstanceImplementationTypeNameRegularExpression, Hint.OnNewInstanceImplementationTypeNameWildcard, GetTypeName),
                (Hint.OnNewInstanceTagRegularExpression, Hint.OnNewInstanceTagWildcard, GetTagName),
                (Hint.OnNewInstanceLifetimeRegularExpression, Hint.OnNewInstanceLifetimeWildcard, GetLifetimeName)))
        {
            return code.Lines;
        }

        var lines = new List<Line>
        {
            new(0, $"{Names.OnNewInstanceMethodName}<{typeResolver.Resolve(ctx.RootContext.Graph.Source, var.InstanceType)}>(ref {var.Name}, {tag.ValueToString()}, {var.Node.Lifetime.ValueToString()});")
        };

        lines.AddRange(code.Lines);
        return lines;
    }

    public string OnInjected(CodeContext ctx, Var var)
    {
        var injection = OnInjectedInternal(ctx, var);
        var refKind = var.Declaration.RefKind switch
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
            var localVarName = $"{var.Name}_{refKind}{idGenerator.Generate()}";
            ctx.Lines.AppendLine($"{var.InstanceType} {localVarName} = {injection};");
            injection = $"{refKind} {localVarName}";
        }

        return injection;
    }

    private string OnInjectedInternal(CodeContext ctx, Var var)
    {
        var variableCode = var.CodeExpression;
        if (variableCode == var.Name)
        {
            var skipNotNullCheck =
                var.InstanceType.IsReferenceType
                && ctx.RootContext.Graph.Source.SemanticModel.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable
                && (var.HasCycle || var.Node.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve);

            if (skipNotNullCheck && (var.HasCycle || var.Node.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve))
            {
                variableCode = $"{variableCode}";
            }
        }

        if (!ctx.RootContext.Graph.Source.Hints.IsOnDependencyInjectionEnabled)
        {
            return variableCode;
        }

        var tag = GetTag(ctx, var);
        string GetTypeName() => typeResolver.Resolve(ctx.RootContext.Graph.Source, var.InstanceType).Name;
        string GetContractName() => symbolNames.GetName(var.ContractType);
        string GetTagName() => tag.ValueToString();
        string GetLifetimeName() => var.Node.Lifetime.ValueToString();
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (!filter.IsMeet(
                ctx.RootContext.Graph.Source,
                (Hint.OnDependencyInjectionImplementationTypeNameRegularExpression, Hint.OnDependencyInjectionImplementationTypeNameWildcard, GetTypeName),
                (Hint.OnDependencyInjectionContractTypeNameRegularExpression, Hint.OnDependencyInjectionContractTypeNameWildcard, GetContractName),
                (Hint.OnDependencyInjectionTagRegularExpression, Hint.OnDependencyInjectionTagWildcard, GetTagName),
                (Hint.OnDependencyInjectionLifetimeRegularExpression, Hint.OnDependencyInjectionLifetimeWildcard, GetLifetimeName)))
        {
            return variableCode;
        }

        return $"{Names.OnDependencyInjectionMethodName}<{typeResolver.Resolve(ctx.RootContext.Graph.Source, var.ContractType)}>({variableCode}, {tag.ValueToString()}, {var.Node.Lifetime.ValueToString()})";
    }

    private static object? GetTag(CodeContext ctx, Var var)
    {
        var tag = var.Injection.Tag;
        if (ReferenceEquals(tag, MdTag.ContextTag))
        {
            tag = ctx.ContextTag;
        }

        return tag;
    }
}