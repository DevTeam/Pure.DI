namespace Pure.DI.Core.CSharp;

internal class ApiMembersBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;
    internal static readonly string ResolveMethodName = nameof(IResolver<object>.ObjectResolve);
    internal static readonly string ResolveByTagMethodName = nameof(IResolver<object>.ObjectResolveByTag);

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
        
        code.AppendLine("#region API");
        if (composition.Source.Source.Settings.GetState(Setting.Resolve, SettingState.On) == SettingState.On)
        {
            AddMethodHeader(code);
            code.AppendLine($"public T {Constant.ResolverMethodName}<T>()");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"return {ResolverInfo.ResolverClassName}<T>.{ResolverClassesBuilder.ResolverPropertyName}!.{ResolverClassesBuilder.ResolveMethodName}(this);");
            }

            code.AppendLine("}");

            code.AppendLine();
            membersCounter++;

            AddMethodHeader(code);
            code.AppendLine($"public T {Constant.ResolverMethodName}<T>(object? tag)");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"return {ResolverInfo.ResolverClassName}<T>.{ResolverClassesBuilder.ResolverPropertyName}!.{ResolverClassesBuilder.ResolveByTagMethodName}(this, tag);");
            }

            code.AppendLine("}");

            code.AppendLine();
            membersCounter++;

            var resolvers = _resolversBuilder.Build(composition.Roots, cancellationToken).ToArray();
            CreateObjectResolverMethod(composition, resolvers, "System.Type type", ResolveMethodName, "this", code);
            membersCounter++;
            code.AppendLine();

            CreateObjectResolverMethod(composition, resolvers, "System.Type type, object? tag", ResolveByTagMethodName, "this, tag", code);
            membersCounter++;
            code.AppendLine();
        }

        if (composition.Source.Source.Settings.GetState(Setting.OnInstanceCreation, SettingState.On) == SettingState.On)
        {
            code.AppendLine(Constant.MethodImplOptions);
            code.AppendLine($"partial void {Constant.OnInstanceCreationMethodName}<T>(ref T value, object? tag, object? lifetime);");
            membersCounter++;
        }

        if (composition.Source.Source.Settings.GetState(Setting.OnDependencyInjection) == SettingState.On)
        {
            code.AppendLine(Constant.MethodImplOptions);
            code.AppendLine($"private partial T {Constant.OnDependencyInjectionMethodName}<T>(in T value, object? tag, object? lifetime);");
            membersCounter++;
        }
        
        code.AppendLine("#endregion");
        return composition with { MembersCount = membersCounter };
    }

    private static void CreateObjectResolverMethod(
        CompositionCode composition,
        IReadOnlyCollection<ResolverInfo> resolvers,
        string methodArgs,
        string resolveMethodName,
        string resolveMethodArgs,
        LinesBuilder code)
    {
        AddMethodHeader(code);
        code.AppendLine($"public object {Constant.ResolverMethodName}({methodArgs})");
        code.AppendLine("{");
        using (code.Indent())
        {
            var divisor = Buckets<object, object>.GetDivisor((uint)resolvers.Count);
            if (resolvers.Any())
            {
                var pairs = $"System.Type, {ResolverClassesBuilder.ResolverInterfaceName}<{composition.Name.ClassName}>";
                var pairTypeName = $"{Constant.ApiNamespace}Pair<{pairs}>";
                if (divisor <= 1)
                {
                    code.AppendLine($"{pairTypeName} pair = {ResolversFieldsBuilder.BucketsFieldName}[0U];");
                }
                else
                {
                    code.AppendLine("#if NETSTANDARD || NETCOREAPP");
                    code.AppendLine($"{pairTypeName} pair = {ResolversFieldsBuilder.BucketsFieldName}[(uint)System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % {divisor}];");
                    code.AppendLine("#else");
                    code.AppendLine($"{pairTypeName} pair = {ResolversFieldsBuilder.BucketsFieldName}[(uint)type.GetHashCode() % {divisor.ToString()}];");
                    code.AppendLine("#endif");
                }

                code.AppendLine("do");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine("if (ReferenceEquals(type, pair.Key))");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine($"return pair.Value.{resolveMethodName}({resolveMethodArgs});");
                    }

                    code.AppendLine("}");
                    code.AppendLine();
                    code.AppendLine("pair = pair.Next;");
                }

                code.AppendLine("} while (pair != null);");
                code.AppendLine();
            }

            code.AppendLine($"throw new System.InvalidOperationException($\"{Constant.CannotResolve} of type {{type}}.\");");
        }
        
        code.AppendLine("}");
    }

    private static void AddMethodHeader(LinesBuilder code)
    {
        code.AppendLine("#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER");
        code.AppendLine("[System.Diagnostics.Contracts.Pure]");
        code.AppendLine("#endif");
        code.AppendLine(Constant.MethodImplOptions);
    }
}