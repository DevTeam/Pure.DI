// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable MoveLocalFunctionAfterJumpStatement
namespace Pure.DI.Core.Code;

using static LinesBuilderExtensions;

sealed class BuildTools(
    IFilter filter,
    ITypeResolver typeResolver,
    IBaseSymbolsProvider baseSymbolsProvider,
    INameProvider nameProvider,
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

    public void AddAggressiveInlining(LinesBuilder code)
    {
        code.AppendLine($"[{Names.MethodImplAttributeName}(({Names.MethodImplOptionsName})256)]");
    }

    public string GetDeclaration(CodeContext ctx, VarDeclaration varDeclaration, string separator = " ", bool useVar = false)
    {
        return varDeclaration.IsDeclared ? "" : $"{(useVar ? "var" : typeResolver.Resolve(ctx.RootContext.Graph.Source, varDeclaration.InstanceType))}{separator}";
    }

    public IEnumerable<Line> OnCreated(CodeContext ctx, VarInjection varInjection)
    {
        if (varInjection.Var.AbstractNode.Arg is not null)
        {
            return [];
        }

        var baseTypes = new Lazy<ImmutableHashSet<ISymbol?>>(() =>
            baseSymbolsProvider.GetBaseSymbols(varInjection.Var.InstanceType, (_, _) => true)
                .Select(i => i.Type)
                .ToImmutableHashSet(SymbolEqualityComparer.Default));

        var accLines = ctx.Accumulators
            .Where(acc => acc.Lifetime == varInjection.Var.AbstractNode.Lifetime)
            .Where(acc => baseTypes.Value.Contains(acc.Type))
            .GroupBy(acc => acc.VarInjection.Var.Name)
            .Select(grouping => grouping.First())
            .OrderBy(i => i.VarInjection.Var.Name)
            .Select(i => new Line(0, $"{i.VarInjection.Var.Name}.Add({varInjection.Var.Name});"))
            .ToList();

        var code = new LinesBuilder();
        if (ctx.IsLockRequired && accLines.Count > 0)
        {
            locks.AddLockStatements(code, false);
            code.AppendLine(BlockStart);
            code.IncIndent();
        }

        code.AppendLines(accLines);

        if (ctx.IsLockRequired && accLines.Count > 0)
        {
            code.DecIndent();
            code.AppendLine(BlockFinish);
        }

        if (!ctx.RootContext.Graph.Source.Hints.IsOnNewInstanceEnabled)
        {
            return code.Lines;
        }

        var tag = GetTag(ctx, varInjection);
        string GetTypeName() => symbolNames.GetName(varInjection.Var.InstanceType);
        string GetTagName() => tag.ValueToString();
        string GetLifetimeName() => varInjection.Var.AbstractNode.Lifetime.ValueToString();
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
            new(0, $"{Names.OnNewInstanceMethodName}<{typeResolver.Resolve(ctx.RootContext.Graph.Source, varInjection.Var.InstanceType)}>(ref {varInjection.Var.Name}, {tag.ValueToString()}, {varInjection.Var.AbstractNode.Lifetime.ValueToString()});")
        };

        lines.AddRange(code.Lines);
        return lines;
    }

    public string OnInjected(CodeContext ctx, VarInjection varInjection)
    {
        var injection = OnInjectedInternal(ctx, varInjection);
        var refKind = varInjection.Var.Declaration.RefKind switch
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
            var localVarName = nameProvider.GetUniqueName($"{varInjection.Var.Name}_{refKind}");
            ctx.Lines.AppendLine($"{varInjection.Var.InstanceType} {localVarName} = {injection};");
            injection = $"{refKind} {localVarName}";
        }

        return injection;
    }

    private string OnInjectedInternal(CodeContext ctx, VarInjection varInjection)
    {
        var variableCode = varInjection.Var.CodeExpression;
        if (variableCode == varInjection.Var.Name)
        {
            var hasCycle = varInjection.Var.HasCycle ?? false;
            var skipNotNullCheck =
                varInjection.Var.InstanceType.IsReferenceType
                && ctx.RootContext.Graph.Source.SemanticModel.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable
                && (hasCycle || varInjection.Var.AbstractNode.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve);

            if (skipNotNullCheck && (hasCycle || varInjection.Var.AbstractNode.Lifetime is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve))
            {
                variableCode = $"{variableCode}";
            }
        }

        if (!ctx.RootContext.Graph.Source.Hints.IsOnDependencyInjectionEnabled)
        {
            return variableCode;
        }

        var tag = GetTag(ctx, varInjection);
        string GetTypeName() => typeResolver.Resolve(ctx.RootContext.Graph.Source, varInjection.Var.InstanceType).Name;
        string GetContractName() => symbolNames.GetName(varInjection.ContractType);
        string GetTagName() => tag.ValueToString();
        string GetLifetimeName() => varInjection.Var.AbstractNode.Lifetime.ValueToString();
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

        return $"{Names.OnDependencyInjectionMethodName}<{typeResolver.Resolve(ctx.RootContext.Graph.Source, varInjection.ContractType)}>({variableCode}, {tag.ValueToString()}, {varInjection.Var.AbstractNode.Lifetime.ValueToString()})";
    }

    private static object? GetTag(CodeContext ctx, VarInjection varInjection)
    {
        var tag = varInjection.Injection.Tag;
        if (ReferenceEquals(tag, MdTag.ContextTag))
        {
            tag = ctx.ContextTag;
        }

        return tag;
    }
}