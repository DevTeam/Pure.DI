// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

sealed class ApiMembersBuilder(
    IBuilder<RootsContext, IEnumerable<ResolverInfo>> resolversBuilder,
    IBuildTools buildTools,
    ICodeNameProvider codeNameProvider)
    : IClassPartBuilder
{
    private const string CommentSummaryStart = "/// <summary>";
    private const string CommentSummary = "/// Resolves the composition root.";
    private const string CommentSummaryByTag = "/// Resolves the composition root by tag.";
    private const string CommentSummaryFinish = "/// </summary>";
    private const string CommentParamType = "/// <param name=\"type\">The type of the composition root.</param>";
    private const string CommentParamTag = "/// <param name=\"tag\">The tag of a composition root.</param>";

    public ClassPart Part => ClassPart.ApiMembers;

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var hints = composition.Hints;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        var apiCode = new Lines();
        var nullable = composition.Compilation.Options.NullableContextOptions == NullableContextOptions.Disable ? "" : "?";
        if (hints.IsResolveEnabled)
        {
            var resolveTypeParam = codeNameProvider.GetUniqueTypeParameterName(composition.Name.ClassName);
            if (isCommentsEnabled)
            {
                apiCode.AppendLine(CommentSummaryStart);
                apiCode.AppendLine(CommentSummary);
                apiCode.AppendLine(CommentSummaryFinish);
                apiCode.AppendLine(GetCommentParamT(resolveTypeParam));
                FinishComments(apiCode);
            }

            buildTools.AddPureHeader(apiCode);
            buildTools.AddAggressiveInlining(apiCode);
            apiCode.AppendLine($"{hints.ResolveMethodModifiers} {resolveTypeParam} {hints.ResolveMethodName}<{resolveTypeParam}>()");
            using (apiCode.CreateBlock())
            {
                apiCode.AppendLine($"return {Names.ResolverClassName}<{resolveTypeParam}>.{Names.ResolverPropertyName}.{Names.ResolveMethodName}(this);");
            }

            membersCounter++;

            apiCode.AppendLine();
            if (isCommentsEnabled)
            {
                apiCode.AppendLine(CommentSummaryStart);
                apiCode.AppendLine(CommentSummaryByTag);
                apiCode.AppendLine(CommentSummaryFinish);
                apiCode.AppendLine(GetCommentParamT(resolveTypeParam));
                apiCode.AppendLine(CommentParamTag);
                FinishComments(apiCode);
            }

            buildTools.AddPureHeader(apiCode);
            buildTools.AddAggressiveInlining(apiCode);
            apiCode.AppendLine($"{hints.ResolveByTagMethodModifiers} {resolveTypeParam} {hints.ResolveByTagMethodName}<{resolveTypeParam}>(object{nullable} tag)");
            using (apiCode.CreateBlock())
            {
                apiCode.AppendLine($"return {Names.ResolverClassName}<{resolveTypeParam}>.{Names.ResolverPropertyName}.{Names.ResolveByTagMethodName}(this, tag);");
            }

            membersCounter++;

            apiCode.AppendLine();
            var resolvers = resolversBuilder.Build(new RootsContext(composition.Setup, composition.PublicRoots)).ToList();
            if (isCommentsEnabled)
            {
                apiCode.AppendLine(CommentSummaryStart);
                apiCode.AppendLine(CommentSummary);
                apiCode.AppendLine(CommentSummaryFinish);
                apiCode.AppendLine(CommentParamType);
                FinishComments(apiCode);
            }

            CreateObjectResolverMethod(
                hints.ObjectResolveMethodModifiers,
                hints.ObjectResolveMethodName,
                resolvers,
                $"{Names.SystemNamespace}Type type",
                Names.ResolveMethodName,
                "this",
                false,
                apiCode);

            membersCounter++;

            if (resolvers.Count > 0)
            {
                apiCode.AppendLine();
                CreateObjectConflictsResolverMethod($"{Names.SystemNamespace}Type type",
                    Names.ResolveMethodName,
                    "this",
                    false,
                    apiCode);

                membersCounter++;
            }

            apiCode.AppendLine();
            if (isCommentsEnabled)
            {
                apiCode.AppendLine(CommentSummaryStart);
                apiCode.AppendLine(CommentSummaryByTag);
                apiCode.AppendLine(CommentSummaryFinish);
                apiCode.AppendLine(CommentParamType);
                apiCode.AppendLine(CommentParamTag);
                FinishComments(apiCode);
            }

            CreateObjectResolverMethod(
                hints.ObjectResolveByTagMethodModifiers,
                hints.ObjectResolveByTagMethodName,
                resolvers,
                $"{Names.SystemNamespace}Type type, object{nullable} tag",
                Names.ResolveByTagMethodName,
                "this, tag",
                true,
                apiCode);

            membersCounter++;

            if (resolvers.Count > 0)
            {
                apiCode.AppendLine();
                CreateObjectConflictsResolverMethod($"{Names.SystemNamespace}Type type, object{nullable} tag",
                    Names.ResolveByTagMethodName,
                    "this, tag",
                    true,
                    apiCode);

                membersCounter++;
            }
        }

        if (composition.Hints is { IsOnNewInstanceEnabled: true, IsOnNewInstancePartial: true })
        {
            apiCode.AppendLine();
            var typeParam = codeNameProvider.GetUniqueTypeParameterName(composition.Name.ClassName);
            apiCode.AppendLine($"partial void {Names.OnNewInstanceMethodName}<{typeParam}>(ref {typeParam} value, object{nullable} tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }

        if (composition.Hints is { IsOnDependencyInjectionEnabled: true, IsOnDependencyInjectionPartial: true })
        {
            apiCode.AppendLine();
            var typeParam = codeNameProvider.GetUniqueTypeParameterName(composition.Name.ClassName);
            apiCode.AppendLine($"private partial {typeParam} {Names.OnDependencyInjectionMethodName}<{typeParam}>(in {typeParam} value, object{nullable} tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }

        if (composition.Hints is { IsOnCannotResolveEnabled: true, IsOnCannotResolvePartial: true })
        {
            apiCode.AppendLine();
            var typeParam = codeNameProvider.GetUniqueTypeParameterName(composition.Name.ClassName);
            apiCode.AppendLine($"private partial {typeParam} {Names.OnCannotResolve}<{typeParam}>(object{nullable} tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }

        // ReSharper disable once InvertIf
        if (apiCode.Count > 0)
        {
            code.AppendLine("#region API");
            code.AppendLines(apiCode);
            code.AppendLine("#endregion");
        }

        return composition with { MembersCount = membersCounter };
    }
    private static void FinishComments(Lines apiCode)
    {
        apiCode.AppendLine("/// <returns>An instance of a composition root.</returns>");
        apiCode.AppendLine($"/// <exception cref=\"{Names.CannotResolveExceptionTypeName}\">Will be thrown if the corresponding composition root was not specified. To specify a composition root use API method such as <see cref=\"{Names.IConfigurationTypeName}.Root{{T}}\"/>.</exception>");
        apiCode.AppendLine($"/// <seealso cref=\"{Names.IConfigurationTypeName}.RootBind{{T}}\"/>");
        apiCode.AppendLine($"/// <seealso cref=\"{Names.IConfigurationTypeName}.Roots{{T}}\"/>");
        apiCode.AppendLine($"/// <seealso cref=\"{Names.IConfigurationTypeName}.Builder{{T}}\"/>");
        apiCode.AppendLine($"/// <seealso cref=\"{Names.IConfigurationTypeName}.Builders{{T}}\"/>");
    }

    private static string GetCommentParamT(string typeParamName) =>
        $"/// <typeparam name=\"{typeParamName}\">The type of the composition root.</typeparam>";

    private void CreateObjectResolverMethod(
        string methodModifiers,
        string methodName,
        IReadOnlyCollection<ResolverInfo> resolvers,
        string methodArgs,
        string resolveMethodName,
        string resolveMethodArgs,
        bool byTag,
        Lines code)
    {
        buildTools.AddPureHeader(code);
        buildTools.AddAggressiveInlining(code);
        code.AppendLine($"{methodModifiers} object {methodName}({methodArgs})");
        using (code.CreateBlock())
        {
            var divisor = Buckets<object>.GetDivisor((uint)resolvers.Count);
            if (resolvers.Count > 0)
            {
                code.AppendLine("#if NETCOREAPP3_0_OR_GREATER");
                code.AppendLine($"var index = (int)({Names.BucketSizeFieldName} * (((uint)type.TypeHandle.GetHashCode()) % {divisor}));");
                code.AppendLine("#else");
                code.AppendLine($"var index = (int)({Names.BucketSizeFieldName} * (((uint){Names.SystemNamespace}Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type)) % {divisor}));");
                code.AppendLine("#endif");
                code.AppendLine($"ref var pair = ref {Names.BucketsFieldName}[index];");
                code.AppendLine($"return {Names.ObjectTypeName}.ReferenceEquals(pair.Key, type) ? pair.Value.{resolveMethodName}({resolveMethodArgs}) : Resolve{Names.Salt}(type, {(byTag ? "tag, " : "")}index);");
            }
            else
            {
                code.AppendLine($"throw new {Names.CannotResolveExceptionTypeName}($\"{{{Names.CannotResolveFieldName}}} {(byTag ? "\\\"{tag}\\\" " : "")}{{{Names.OfTypeFieldName}}} {{type}}.\", type, {(byTag ? "tag" : "null")});");
            }
        }
    }

    private void CreateObjectConflictsResolverMethod(
        string methodArgs,
        string resolveMethodName,
        string resolveMethodArgs,
        bool byTag,
        Lines code)
    {
        buildTools.AddNoInlining(code);
        code.AppendLine($"private object Resolve{Names.Salt}({methodArgs}, int index)");
        using (code.CreateBlock())
        {
            code.AppendLine($"var finish = index + {Names.BucketSizeFieldName};");
            code.AppendLine("while (++index < finish)");
            using (code.CreateBlock())
            {
                code.AppendLine($"ref var pair = ref {Names.BucketsFieldName}[index];");
                code.AppendLine($"if ({Names.ObjectTypeName}.ReferenceEquals(pair.Key, type))");
                using (code.CreateBlock())
                {
                    code.AppendLine($"return pair.Value.{resolveMethodName}({resolveMethodArgs});");
                }
            }

            code.AppendLine();
            code.AppendLine($"throw new {Names.ApiNamespace}{nameof(CannotResolveException)}($\"{{{Names.CannotResolveFieldName}}} {(byTag ? "\\\"{tag}\\\" " : "")}{{{Names.OfTypeFieldName}}} {{type}}.\", type, {(byTag ? "tag" : "null")});");
        }
    }
}
