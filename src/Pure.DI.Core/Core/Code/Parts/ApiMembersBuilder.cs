// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

internal sealed class ApiMembersBuilder(
    IBuilder<RootContext, IEnumerable<ResolverInfo>> resolversBuilder,
    IBuildTools buildTools)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.ApiMembers;

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var hints = composition.Source.Source.Hints;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        var apiCode = new LinesBuilder();
        var nullable = composition.Compilation.Options.NullableContextOptions == NullableContextOptions.Disable ? "" : "?";
        if (hints.IsResolveEnabled)
        {
            if (isCommentsEnabled)
            {
                apiCode.AppendLine("/// <summary>");
                apiCode.AppendLine("/// Resolves the composition root.");
                apiCode.AppendLine("/// </summary>");
                apiCode.AppendLine("/// <typeparam name=\"T\">The type of the composition root.</typeparam>");
                apiCode.AppendLine("/// <returns>A composition root.</returns>");
            }

            buildTools.AddPureHeader(apiCode);
            buildTools.AddAggressiveInlining(apiCode);
            apiCode.AppendLine($"{hints.ResolveMethodModifiers} T {hints.ResolveMethodName}<T>()");
            apiCode.AppendLine("{");
            using (apiCode.Indent())
            {
                apiCode.AppendLine($"return {Names.ResolverClassName}<T>.{Names.ResolverPropertyName}.{Names.ResolveMethodName}(this);");
            }

            apiCode.AppendLine("}");
            membersCounter++;

            apiCode.AppendLine();
            if (isCommentsEnabled)
            {
                apiCode.AppendLine("/// <summary>");
                apiCode.AppendLine("/// Resolves the composition root by tag.");
                apiCode.AppendLine("/// </summary>");
                apiCode.AppendLine("/// <typeparam name=\"T\">The type of the composition root.</typeparam>");
                apiCode.AppendLine("/// <param name=\"tag\">The tag of a composition root.</param>");
                apiCode.AppendLine("/// <returns>A composition root.</returns>");
            }

            buildTools.AddPureHeader(apiCode);
            buildTools.AddAggressiveInlining(apiCode);
            apiCode.AppendLine($"{hints.ResolveByTagMethodModifiers} T {hints.ResolveByTagMethodName}<T>(object{nullable} tag)");
            apiCode.AppendLine("{");
            using (apiCode.Indent())
            {
                apiCode.AppendLine($"return {Names.ResolverClassName}<T>.{Names.ResolverPropertyName}.{Names.ResolveByTagMethodName}(this, tag);");
            }

            apiCode.AppendLine("}");
            membersCounter++;

            apiCode.AppendLine();
            var resolvers = resolversBuilder.Build(new RootContext(composition.Source.Source, composition.Roots)).ToList();
            if (isCommentsEnabled)
            {
                apiCode.AppendLine("/// <summary>");
                apiCode.AppendLine("/// Resolves the composition root.");
                apiCode.AppendLine("/// </summary>");
                apiCode.AppendLine("/// <param name=\"type\">The type of the composition root.</param>");
                apiCode.AppendLine("/// <returns>A composition root.</returns>");
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
                apiCode.AppendLine("/// <summary>");
                apiCode.AppendLine("/// Resolves the composition root by tag.");
                apiCode.AppendLine("/// </summary>");
                apiCode.AppendLine("/// <param name=\"type\">The type of the composition root.</param>");
                apiCode.AppendLine("/// <param name=\"tag\">The tag of a composition root.</param>");
                apiCode.AppendLine("/// <returns>A composition root.</returns>");
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
            code.AppendLines(apiCode.Lines);
            code.AppendLine("#endregion");
        }

        return composition with { MembersCount = membersCounter };
    }

    private void CreateObjectResolverMethod(
        string methodModifiers,
        string methodName,
        IReadOnlyCollection<ResolverInfo> resolvers,
        string methodArgs,
        string resolveMethodName,
        string resolveMethodArgs,
        bool byTag,
        LinesBuilder code)
    {
        buildTools.AddPureHeader(code);
        buildTools.AddAggressiveInlining(code);
        code.AppendLine($"{methodModifiers} object {methodName}({methodArgs})");
        code.AppendLine("{");
        using (code.Indent())
        {
            var divisor = Buckets<object, object>.GetDivisor((uint)resolvers.Count);
            if (resolvers.Count > 0)
            {
                code.AppendLine($"var index = (int)({Names.BucketSizeFieldName} * ((uint){Names.SystemNamespace}Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % {divisor}));");
                code.AppendLine($"ref var pair = ref {Names.BucketsFieldName}[index];");
                code.AppendLine($"return pair.Key == type ? pair.Value.{resolveMethodName}({resolveMethodArgs}) : Resolve{Names.Salt}(type, {(byTag ? "tag, " : "")}index);");
            }
            else
            {
                code.AppendLine($"throw new {Names.SystemNamespace}InvalidOperationException($\"{{{Names.CannotResolveFieldName}}} {(byTag ? "\\\"{tag}\\\" " : "")}{{{Names.OfTypeFieldName}}} {{type}}.\");");
            }
        }

        code.AppendLine("}");
    }

    private static void CreateObjectConflictsResolverMethod(
        string methodArgs,
        string resolveMethodName,
        string resolveMethodArgs,
        bool byTag,
        LinesBuilder code)
    {
        code.AppendLine($"[{Names.MethodImplAttributeName}({Names.MethodImplNoInlining})]");
        code.AppendLine($"private object Resolve{Names.Salt}({methodArgs}, int index)");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"var finish = index + {Names.BucketSizeFieldName};");
            code.AppendLine("while (++index < finish)");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"ref var pair = ref {Names.BucketsFieldName}[index];");
                code.AppendLine("if (pair.Key == type)");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine($"return pair.Value.{resolveMethodName}({resolveMethodArgs});");
                }

                code.AppendLine("}");
            }

            code.AppendLine("}");
            code.AppendLine();

            code.AppendLine($"throw new {Names.SystemNamespace}InvalidOperationException($\"{{{Names.CannotResolveFieldName}}} {(byTag ? "\\\"{tag}\\\" " : "")}{{{Names.OfTypeFieldName}}} {{type}}.\");");
        }

        code.AppendLine("}");
    }
}