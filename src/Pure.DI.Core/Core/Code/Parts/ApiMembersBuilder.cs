// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

sealed class ApiMembersBuilder(
    IBuilder<RootsContext, IEnumerable<ResolverInfo>> resolversBuilder,
    IBuildTools buildTools)
    : IClassPartBuilder
{
    private const string CommentSummaryStart = "/// <summary>";
    private const string CommentSummary = "/// Resolves the composition root.";
    private const string CommentSummaryByTag = "/// Resolves the composition root by tag.";
    private const string CommentSummaryFinish = "/// </summary>";
    private const string CommentParamT = "/// <typeparam name=\"T\">The type of the composition root.</typeparam>";
    private const string CommentParamType = "/// <param name=\"type\">The type of the composition root.</param>";
    private const string CommentParamTag = "/// <param name=\"tag\">The tag of a composition root.</param>";

    public ClassPart Part => ClassPart.ApiMembers;

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var hints = composition.Source.Source.Hints;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        var apiCode = new Lines();
        var nullable = composition.Compilation.Options.NullableContextOptions == NullableContextOptions.Disable ? "" : "?";
        if (hints.IsResolveEnabled)
        {
            if (isCommentsEnabled)
            {
                apiCode.AppendLine(CommentSummaryStart);
                apiCode.AppendLine(CommentSummary);
                apiCode.AppendLine(CommentSummaryFinish);
                apiCode.AppendLine(CommentParamT);
                FinishComments(apiCode);
            }

            buildTools.AddPureHeader(apiCode);
            buildTools.AddAggressiveInlining(apiCode);
            apiCode.AppendLine($"{hints.ResolveMethodModifiers} T {hints.ResolveMethodName}<T>()");
            using (apiCode.CreateBlock())
            {
                apiCode.AppendLine($"return {Names.ResolverClassName}<T>.{Names.ResolverPropertyName}.{Names.ResolveMethodName}(this);");
            }

            membersCounter++;

            apiCode.AppendLine();
            if (isCommentsEnabled)
            {
                apiCode.AppendLine(CommentSummaryStart);
                apiCode.AppendLine(CommentSummaryByTag);
                apiCode.AppendLine(CommentSummaryFinish);
                apiCode.AppendLine(CommentParamT);
                apiCode.AppendLine(CommentParamTag);
                FinishComments(apiCode);
            }

            buildTools.AddPureHeader(apiCode);
            buildTools.AddAggressiveInlining(apiCode);
            apiCode.AppendLine($"{hints.ResolveByTagMethodModifiers} T {hints.ResolveByTagMethodName}<T>(object{nullable} tag)");
            using (apiCode.CreateBlock())
            {
                apiCode.AppendLine($"return {Names.ResolverClassName}<T>.{Names.ResolverPropertyName}.{Names.ResolveByTagMethodName}(this, tag);");
            }

            membersCounter++;

            apiCode.AppendLine();
            var resolvers = resolversBuilder.Build(new RootsContext(composition.Source.Source, composition.PublicRoots)).ToList();
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

        if (composition.Source.Source.Hints is { IsOnNewInstanceEnabled: true, IsOnNewInstancePartial: true })
        {
            apiCode.AppendLine();
            apiCode.AppendLine($"partial void {Names.OnNewInstanceMethodName}<T>(ref T value, object{nullable} tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }

        if (composition.Source.Source.Hints is { IsOnDependencyInjectionEnabled: true, IsOnDependencyInjectionPartial: true })
        {
            apiCode.AppendLine();
            apiCode.AppendLine($"private partial T {Names.OnDependencyInjectionMethodName}<T>(in T value, object{nullable} tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }

        if (composition.Source.Source.Hints is { IsOnCannotResolveEnabled: true, IsOnCannotResolvePartial: true })
        {
            apiCode.AppendLine();
            apiCode.AppendLine($"private partial T {Names.OnCannotResolve}<T>(object{nullable} tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
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
        apiCode.AppendLine($"/// <exception cref=\"{Names.ApiNamespace}CannotResolveException\">Will be thrown if the corresponding composition root was not specified. To specify a composition root use API method such as <see cref=\"{Names.IConfigurationTypeName}.Root{{T}}\"/>.</exception>");
        apiCode.AppendLine($"/// <seealso cref=\"{Names.IConfigurationTypeName}.RootBind{{T}}\"/>");
        apiCode.AppendLine($"/// <seealso cref=\"{Names.IConfigurationTypeName}.Roots{{T}}\"/>");
        apiCode.AppendLine($"/// <seealso cref=\"{Names.IConfigurationTypeName}.Builder{{T}}\"/>");
        apiCode.AppendLine($"/// <seealso cref=\"{Names.IConfigurationTypeName}.Builders{{T}}\"/>");
    }

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
                code.AppendLine($"var index = (int)({Names.BucketSizeFieldName} * ((uint){Names.SystemNamespace}Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % {divisor}));");
                code.AppendLine($"ref var pair = ref {Names.BucketsFieldName}[index];");
                code.AppendLine($"return pair.Key == type ? pair.Value.{resolveMethodName}({resolveMethodArgs}) : Resolve{Names.Salt}(type, {(byTag ? "tag, " : "")}index);");
            }
            else
            {
                code.AppendLine($"throw new {Names.ApiNamespace}{nameof(CannotResolveException)}($\"{{{Names.CannotResolveFieldName}}} {(byTag ? "\\\"{tag}\\\" " : "")}{{{Names.OfTypeFieldName}}} {{type}}.\");");
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
                code.AppendLine("if (pair.Key == type)");
                using (code.CreateBlock())
                {
                    code.AppendLine($"return pair.Value.{resolveMethodName}({resolveMethodArgs});");
                }
            }

            code.AppendLine();
            code.AppendLine($"throw new {Names.ApiNamespace}{nameof(CannotResolveException)}($\"{{{Names.CannotResolveFieldName}}} {(byTag ? "\\\"{tag}\\\" " : "")}{{{Names.OfTypeFieldName}}} {{type}}.\");");
        }
    }
}