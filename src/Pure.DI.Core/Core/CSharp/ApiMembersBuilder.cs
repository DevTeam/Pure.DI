namespace Pure.DI.Core.CSharp;

internal class ApiMembersBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;
    
    public ApiMembersBuilder(
        IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
    {
        _resolversBuilder = resolversBuilder;
    }

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        var settings = composition.Source.Source.Hints;
        code.AppendLine("#region API");
        if (settings.GetHint(Hint.Resolve, SettingState.On) == SettingState.On)
        {
            AddMethodHeader(code);
            code.AppendLine($"{settings.GetValueOrDefault(Hint.ResolveMethodModifiers, Constant.DefaultApiMethodModifiers)} T {settings.GetValueOrDefault(Hint.ResolveMethodName, Constant.ResolverMethodName)}<T>()");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"return {ResolverInfo.ResolverClassName}<T>.{ResolverClassesBuilder.ResolverPropertyName}.{ResolverClassesBuilder.ResolveMethodName}(this);");
            }

            code.AppendLine("}");

            code.AppendLine();
            membersCounter++;

            AddMethodHeader(code);
            code.AppendLine($"{settings.GetValueOrDefault(Hint.ResolveByTagMethodModifiers, Constant.DefaultApiMethodModifiers)} T {settings.GetValueOrDefault(Hint.ResolveByTagMethodName, Constant.ResolverMethodName)}<T>(object? tag)");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"return {ResolverInfo.ResolverClassName}<T>.{ResolverClassesBuilder.ResolverPropertyName}.{ResolverClassesBuilder.ResolveByTagMethodName}(this, tag);");
            }

            code.AppendLine("}");

            code.AppendLine();
            membersCounter++;

            var resolvers = _resolversBuilder.Build(composition.Roots, cancellationToken).ToArray();
            CreateObjectResolverMethod(
                settings.GetValueOrDefault(Hint.ObjectResolveMethodModifiers, Constant.DefaultApiMethodModifiers),
                settings.GetValueOrDefault(Hint.ObjectResolveMethodName, Constant.ResolverMethodName),
                resolvers,
                $"{Constant.SystemNamespace}Type type",
                ResolverClassesBuilder.ResolveMethodName,
                "this",
                code);
            
            membersCounter++;
            code.AppendLine();

            CreateObjectResolverMethod(
                settings.GetValueOrDefault(Hint.ObjectResolveByTagMethodModifiers, Constant.DefaultApiMethodModifiers),
                settings.GetValueOrDefault(Hint.ObjectResolveByTagMethodName, Constant.ResolverMethodName),
                resolvers,
                $"{Constant.SystemNamespace}Type type, object? tag",
                ResolverClassesBuilder.ResolveByTagMethodName,
                "this, tag",
                code);

            membersCounter++;
            code.AppendLine();
        }

        if (composition.Source.Source.Hints.GetHint(Hint.OnInstanceCreation) == SettingState.On)
        {
            code.AppendLine(Constant.MethodImplOptions);
            code.AppendLine($"partial void {Constant.OnInstanceCreationMethodName}<T>(ref T value, object? tag, {Constant.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }

        if (composition.Source.Source.Hints.GetHint(Hint.OnDependencyInjection) == SettingState.On)
        {
            code.AppendLine(Constant.MethodImplOptions);
            code.AppendLine($"private partial T {Constant.OnDependencyInjectionMethodName}<T>(in T value, object? tag, {Constant.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }
        
        if (composition.Source.Source.Hints.GetHint(Hint.OnCannotResolve) == SettingState.On)
        {
            code.AppendLine(Constant.MethodImplOptions);
            code.AppendLine($"private partial T {Constant.OnCannotResolve}<T>(object? tag, {Constant.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }
        
        code.AppendLine("#endregion");
        return composition with { MembersCount = membersCounter };
    }

    private static void CreateObjectResolverMethod(
        string methodModifiers,
        string methodName,
        IReadOnlyCollection<ResolverInfo> resolvers,
        string methodArgs,
        string resolveMethodName,
        string resolveMethodArgs,
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
                code.AppendLine($"int index = (int)({ResolversFieldsBuilder.BucketSizeFieldName} * ((uint){Constant.SystemNamespace}Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % {divisor}));");
                code.AppendLine($"ref var pair = ref {ResolversFieldsBuilder.BucketsFieldName}[index];");
                code.AppendLine("if (ReferenceEquals(pair.Key, type))");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine($"return pair.Value.{resolveMethodName}({resolveMethodArgs});");
                }

                code.AppendLine("}");
                code.AppendLine();
                code.AppendLine($"int maxIndex = index + {ResolversFieldsBuilder.BucketSizeFieldName};");
                code.AppendLine("for (int i = index + 1; i < maxIndex; i++)");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine($"pair = ref {ResolversFieldsBuilder.BucketsFieldName}[i];");
                    code.AppendLine("if (ReferenceEquals(pair.Key, type))");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine($"return pair.Value.{resolveMethodName}({resolveMethodArgs});");
                    }
                    
                    code.AppendLine("}");
                }
                code.AppendLine("}");
                code.AppendLine();
            }

            code.AppendLine($"throw new {Constant.SystemNamespace}InvalidOperationException($\"{Constant.CannotResolve} of type {{type}}.\");");
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