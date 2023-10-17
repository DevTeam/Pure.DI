// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ApiMembersBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly ILogger<ApiMembersBuilder> _logger;
    private readonly IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;
    
    public ApiMembersBuilder(
        ILogger<ApiMembersBuilder> logger,
        IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
    {
        _logger = logger;
        _resolversBuilder = resolversBuilder;
    }

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        var hints = composition.Source.Source.Hints;
        var apiCode = new LinesBuilder();
        if (hints.GetHint(Hint.Resolve, SettingState.On) == SettingState.On)
        {
            var rootArgs = composition.Args.Where(i => i.Node.Arg?.Source.Kind == ArgKind.Root).ToArray();
            foreach (var rootArg in rootArgs)
            {
                _logger.CompileWarning($"The root argument \"{rootArg.Node.Arg}\" of the composition is used. This root cannot be resolved using \"Resolve\" methods, so an exception will be thrown when trying to do so.", rootArg.Node.Arg?.Source.Source.GetLocation() ?? composition.Source.Source.Source.GetLocation(), LogId.WarningRootArgInResolveMethod);
            }
            
            AddMethodHeader(apiCode);
            apiCode.AppendLine($"{hints.GetValueOrDefault(Hint.ResolveMethodModifiers, Names.DefaultApiMethodModifiers)} T {hints.GetValueOrDefault(Hint.ResolveMethodName, Names.ResolveMethodName)}<T>()");
            apiCode.AppendLine("{");
            using (apiCode.Indent())
            {
                apiCode.AppendLine($"return {Names.ResolverClassName}<T>.{Names.ResolverPropertyName}.{Names.ResolveMethodName}(this);");
            }

            apiCode.AppendLine("}");

            apiCode.AppendLine();
            membersCounter++;

            AddMethodHeader(apiCode);
            apiCode.AppendLine($"{hints.GetValueOrDefault(Hint.ResolveByTagMethodModifiers, Names.DefaultApiMethodModifiers)} T {hints.GetValueOrDefault(Hint.ResolveByTagMethodName, Names.ResolveMethodName)}<T>(object? tag)");
            apiCode.AppendLine("{");
            using (apiCode.Indent())
            {
                apiCode.AppendLine($"return {Names.ResolverClassName}<T>.{Names.ResolverPropertyName}.{Names.ResolveByTagMethodName}(this, tag);");
            }

            apiCode.AppendLine("}");

            apiCode.AppendLine();
            membersCounter++;

            var resolvers = _resolversBuilder.Build(composition.Roots).ToArray();
            CreateObjectResolverMethod(
                hints.GetValueOrDefault(Hint.ObjectResolveMethodModifiers, Names.DefaultApiMethodModifiers),
                hints.GetValueOrDefault(Hint.ObjectResolveMethodName, Names.ResolveMethodName),
                resolvers,
                $"{Names.SystemNamespace}Type type",
                Names.ResolveMethodName,
                "this",
                false,
                apiCode);
            
            membersCounter++;
            
            apiCode.AppendLine();
            CreateObjectResolverMethod(
                hints.GetValueOrDefault(Hint.ObjectResolveByTagMethodModifiers, Names.DefaultApiMethodModifiers),
                hints.GetValueOrDefault(Hint.ObjectResolveByTagMethodName, Names.ResolveMethodName),
                resolvers,
                $"{Names.SystemNamespace}Type type, object? tag",
                Names.ResolveByTagMethodName,
                "this, tag",
                true,
                apiCode);

            membersCounter++;
        }

        if (composition.Source.Source.Hints.GetHint(Hint.OnNewInstance) == SettingState.On
            && composition.Source.Source.Hints.GetHint(Hint.OnNewInstancePartial, SettingState.On) == SettingState.On)
        {
            apiCode.AppendLine();
            apiCode.AppendLine(Names.MethodImplOptions);
            apiCode.AppendLine($"partial void {Names.OnNewInstanceMethodName}<T>(ref T value, object? tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }

        if (composition.Source.Source.Hints.GetHint(Hint.OnDependencyInjection) == SettingState.On
            && composition.Source.Source.Hints.GetHint(Hint.OnDependencyInjectionPartial, SettingState.On) == SettingState.On)
        {
            apiCode.AppendLine();
            apiCode.AppendLine(Names.MethodImplOptions);
            apiCode.AppendLine($"private partial T {Names.OnDependencyInjectionMethodName}<T>(in T value, object? tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }
        
        if (composition.Source.Source.Hints.GetHint(Hint.OnCannotResolve) == SettingState.On
            && composition.Source.Source.Hints.GetHint(Hint.OnCannotResolvePartial, SettingState.On) == SettingState.On)
        {
            apiCode.AppendLine();
            apiCode.AppendLine(Names.MethodImplOptions);
            apiCode.AppendLine($"private partial T {Names.OnCannotResolve}<T>(object? tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }

        // ReSharper disable once InvertIf
        if (apiCode.Count > 0)
        {
            code.AppendLine("#region API");
            code.AppendLines(apiCode.Lines);
            code.AppendLine("#endregion");
        }

        return composition with { MembersCount = membersCounter };
    }

    private static void CreateObjectResolverMethod(
        string methodModifiers,
        string methodName,
        IReadOnlyCollection<ResolverInfo> resolvers,
        string methodArgs,
        string resolveMethodName,
        string resolveMethodArgs,
        bool byTag,
        LinesBuilder code)
    {
        AddMethodHeader(code);
        code.AppendLine($"{methodModifiers} object {methodName}({methodArgs})");
        code.AppendLine("{");
        using (code.Indent())
        {
            var divisor = Buckets<object, object>.GetDivisor((uint)resolvers.Count);
            if (resolvers.Any())
            {
                code.AppendLine($"var index = (int)({Names.BucketSizeFieldName} * ((uint){Names.SystemNamespace}Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % {divisor}));");
                code.AppendLine($"var finish = index + {Names.BucketSizeFieldName};");
                code.AppendLine("do {");
                using (code.Indent())
                {
                    code.AppendLine($"ref var pair = ref {Names.BucketsFieldName}[index];");
                    code.AppendLine("if (ReferenceEquals(pair.Key, type))");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine($"return pair.Value.{resolveMethodName}({resolveMethodArgs});");
                    }
                    
                    code.AppendLine("}");
                }

                code.AppendLine("} while (++index < finish);");
                code.AppendLine();
            }

            code.AppendLine($"throw new {Names.SystemNamespace}InvalidOperationException($\"{Names.CannotResolve} {(byTag ? "\\\"{tag}\\\" " : "")}of type {{type}}.\");");
        }
        
        code.AppendLine("}");
    }

    private static void AddMethodHeader(LinesBuilder code)
    {
        code.AppendLine("#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER");
        code.AppendLine($"[{Names.SystemNamespace}Diagnostics.Contracts.Pure]");
        code.AppendLine("#endif");
        code.AppendLine(Names.MethodImplOptions);
    }
}