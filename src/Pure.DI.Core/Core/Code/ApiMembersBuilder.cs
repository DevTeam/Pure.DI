// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ApiMembersBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;
    
    public ApiMembersBuilder(
        IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
    {
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
            AddMethodHeader(apiCode);
            apiCode.AppendLine($"{hints.GetValueOrDefault(Hint.ResolveMethodModifiers, Constant.DefaultApiMethodModifiers)} T {hints.GetValueOrDefault(Hint.ResolveMethodName, Constant.ResolverMethodName)}<T>()");
            apiCode.AppendLine("{");
            using (apiCode.Indent())
            {
                apiCode.AppendLine($"return {ResolverInfo.ResolverClassName}<T>.{ResolverClassesBuilder.ResolverPropertyName}.{ResolverClassesBuilder.ResolveMethodName}(this);");
            }

            apiCode.AppendLine("}");

            apiCode.AppendLine();
            membersCounter++;

            AddMethodHeader(apiCode);
            apiCode.AppendLine($"{hints.GetValueOrDefault(Hint.ResolveByTagMethodModifiers, Constant.DefaultApiMethodModifiers)} T {hints.GetValueOrDefault(Hint.ResolveByTagMethodName, Constant.ResolverMethodName)}<T>(object? tag)");
            apiCode.AppendLine("{");
            using (apiCode.Indent())
            {
                apiCode.AppendLine($"return {ResolverInfo.ResolverClassName}<T>.{ResolverClassesBuilder.ResolverPropertyName}.{ResolverClassesBuilder.ResolveByTagMethodName}(this, tag);");
            }

            apiCode.AppendLine("}");

            apiCode.AppendLine();
            membersCounter++;

            var resolvers = _resolversBuilder.Build(composition.Roots).ToArray();
            CreateObjectResolverMethod(
                hints.GetValueOrDefault(Hint.ObjectResolveMethodModifiers, Constant.DefaultApiMethodModifiers),
                hints.GetValueOrDefault(Hint.ObjectResolveMethodName, Constant.ResolverMethodName),
                resolvers,
                $"{Constant.SystemNamespace}Type type",
                ResolverClassesBuilder.ResolveMethodName,
                "this",
                false,
                apiCode);
            
            membersCounter++;
            
            apiCode.AppendLine();
            CreateObjectResolverMethod(
                hints.GetValueOrDefault(Hint.ObjectResolveByTagMethodModifiers, Constant.DefaultApiMethodModifiers),
                hints.GetValueOrDefault(Hint.ObjectResolveByTagMethodName, Constant.ResolverMethodName),
                resolvers,
                $"{Constant.SystemNamespace}Type type, object? tag",
                ResolverClassesBuilder.ResolveByTagMethodName,
                "this, tag",
                true,
                apiCode);

            membersCounter++;
        }

        if (composition.Source.Source.Hints.GetHint(Hint.OnNewInstance) == SettingState.On)
        {
            apiCode.AppendLine();
            apiCode.AppendLine(Constant.MethodImplOptions);
            apiCode.AppendLine($"partial void {Constant.OnNewInstanceMethodName}<T>(ref T value, object? tag, {Constant.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }

        if (composition.Source.Source.Hints.GetHint(Hint.OnDependencyInjection) == SettingState.On)
        {
            apiCode.AppendLine();
            apiCode.AppendLine(Constant.MethodImplOptions);
            apiCode.AppendLine($"private partial T {Constant.OnDependencyInjectionMethodName}<T>(in T value, object? tag, {Constant.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }
        
        if (composition.Source.Source.Hints.GetHint(Hint.OnCannotResolve) == SettingState.On)
        {
            apiCode.AppendLine();
            apiCode.AppendLine(Constant.MethodImplOptions);
            apiCode.AppendLine($"private partial T {Constant.OnCannotResolve}<T>(object? tag, {Constant.ApiNamespace}{nameof(Lifetime)} lifetime);");
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
                code.AppendLine($"var index = (int)({ResolversFieldsBuilder.BucketSizeFieldName} * ((uint){Constant.SystemNamespace}Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % {divisor}));");
                code.AppendLine($"var finish = index + {ResolversFieldsBuilder.BucketSizeFieldName};");
                code.AppendLine("do {");
                using (code.Indent())
                {
                    code.AppendLine($"ref var pair = ref {ResolversFieldsBuilder.BucketsFieldName}[index];");
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

            code.AppendLine($"throw new {Constant.SystemNamespace}InvalidOperationException($\"{Constant.CannotResolve} {(byTag ? "\\\"{tag}\\\" " : "")}of type {{type}}.\");");
        }
        
        code.AppendLine("}");
    }

    private static void AddMethodHeader(LinesBuilder code)
    {
        code.AppendLine("#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER");
        code.AppendLine($"[{Constant.SystemNamespace}Diagnostics.Contracts.Pure]");
        code.AppendLine("#endif");
        code.AppendLine(Constant.MethodImplOptions);
    }
}