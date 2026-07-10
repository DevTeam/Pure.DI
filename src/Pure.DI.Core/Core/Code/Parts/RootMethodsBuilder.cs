// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

using static LinesExtensions;

sealed class RootMethodsBuilder(
    IBuildTools buildTools,
    IRootSignatureProvider rootSignatureProvider,
    [Tag(typeof(RootMethodsCommenter))] ICommenter<Root> rootCommenter,
    IRootAccessModifierResolver rootAccessModifierResolver,
    IMarker marker,
    IUniqueNameProvider uniqueNameProvider,
    INameFormatter nameFormatter,
    ITypeResolver typeResolver,
    IRefSafety refSafety,
    CancellationToken cancellationToken)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.RootMethods;

    public CompositionCode Build(CompositionCode composition)
    {
        if (composition.PublicRoots.Length == 0)
        {
            return composition;
        }

        var code = composition.Code;
        var generatePrivateRoots = composition.Hints.IsResolveEnabled;
        var membersCounter = composition.MembersCount;
        code.AppendLine("#region Roots");
        var isFirst = true;
        var roots = composition.PublicRoots.Where(i => generatePrivateRoots || i.IsPublic);
        foreach (var root in roots)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                code.AppendLine();
            }

            BuildRoot(composition, root);
            membersCounter++;
            // ReSharper disable once InvertIf
            if (!root.Source.BuilderRoots.IsDefaultOrEmpty)
            {
                code.AppendLine();
                BuildTryRoot(composition, root);
                membersCounter++;
            }
        }

        code.AppendLine("#endregion");
        return composition with { MembersCount = membersCounter };
    }

    private void BuildRoot(CompositionCode composition, Root root)
    {
        var constraints = rootSignatureProvider.TryGetConstraints(composition, root);
        if (constraints is null)
        {
            return;
        }

        rootCommenter.AddComments(composition, root);
        var code = composition.Code;
        if (root is { IsMethod: true, Source.IsBuilder: false })
        {
            buildTools.AddPureHeader(code);
        }

        if ((root.Kind & RootKinds.Exported) == RootKinds.Exported)
        {
            var tag = root.Injection.Tag;
            if (tag == MdTag.ContextTag)
            {
                tag = null;
            }

            if (tag is not null)
            {
                code.AppendLine($"[{Names.ExportAttributeName}(typeof({GetAttributeType(composition, root)}), {Names.GeneratorName}.{nameof(Lifetime)}.{nameof(Lifetime.Transient)}, {tag.ValueToString()})]");
            }
            else
            {
                if (root.IsMethod && marker.IsMarkerBased(composition.Setup, root.Injection.Type))
                {
                    code.AppendLine($"[{Names.ExportAttributeName}(typeof({GetAttributeType(composition, root)}))]");
                }
                else
                {
                    code.AppendLine($"[{Names.ExportAttributeName}]");
                }
            }
        }

        if (root.IsMethod)
        {
            if (!root.Source.BuilderRoots.IsDefaultOrEmpty)
            {
                // Common builder
                code.AppendLine("#pragma warning disable CS0162");
                buildTools.AddNoInlining(code);
            }
            else
            {
                buildTools.AddAggressiveInlining(code);
            }
        }

        code.AppendLine(rootSignatureProvider.GetRootSignature(composition, root));
        if (!constraints.IsEmpty)
        {
            using (code.Indent())
            {
                foreach (var constraint in constraints.OrderBy(i => i.Key.Name))
                {
                    code.AppendLine($"where {constraint.Key.Name}: {string.Join(", ", constraint.Value)}");
                }
            }
        }

        using (code.CreateBlock())
        {
            var indentToken = Disposables.Empty;
            if (root.IsMethod)
            {
                foreach (var arg in root.RootArgs.Where(i => i.InstanceType.IsReferenceType && i.InstanceType.NullableAnnotation != NullableAnnotation.Annotated))
                {
                    code.AppendLine($"if ({buildTools.NullCheck(composition.Compilation, arg.Name)}) throw new {Names.SystemNamespace}ArgumentNullException(nameof({arg.Name}));");
                }
            }
            else
            {
                buildTools.AddAggressiveInlining(code);
                code.AppendLine("get");
                code.AppendLine(BlockStart);
                indentToken = code.Indent();
            }

            try
            {
                if (!root.Source.BuilderRoots.IsDefaultOrEmpty)
                {
                    code.AppendLine($"if (Try{root.DisplayName}{GetTypeArguments(root)}({string.Join(", ", root.RootArgs.Select(i => i.Name))}))");
                    using (code.CreateBlock())
                    {
                        code.AppendLine($"return {Names.BuildingInstance};");
                    }

                    code.AppendLine($"throw new {Names.ArgumentExceptionTypeName}($\"{Names.CannotBuildMessage} {{{Names.BuildingInstance}.GetType()}}.\", \"{Names.BuildingInstance}\");");
                }
                else
                {
                    Lines lines;
                    if (root.Kind.HasFlag(RootKinds.Light))
                    {
                        lines = new Lines();
                        var compositionTypeName = composition.Name.ClassName;
                        var compositionInstance = root.IsStatic ? $"new {compositionTypeName}()." : string.Empty;
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (root.RootArgs.IsEmpty)
                        {
                            lines.AppendLine($"return {compositionInstance}{Names.LightweightRootName}.{root.Source.UniqueName}();");
                        }
                        else
                        {
                            lines.AppendLine($"return {compositionInstance}{Names.LightweightRootName}({string.Join(", ", root.RootArgs.Select(i => i.Name))}).{root.Source.UniqueName}();");
                        }
                    }
                    else
                    {
                        lines = root.Lines;
                    }

                    if (composition.Hints.IsFormatCodeEnabled)
                    {
                        var codeText = string.Join(Environment.NewLine, lines);
                        var syntaxTree = CSharpSyntaxTree.ParseText(codeText, cancellationToken: cancellationToken);
                        foreach (var line in syntaxTree.GetRoot().NormalizeWhitespace("\t", Environment.NewLine).GetText().Lines)
                        {
                            code.AppendLine(line.ToString());
                        }
                    }
                    else
                    {
                        code.AppendLines(lines);
                    }
                }
            }
            finally
            {
                indentToken.Dispose();
            }

            if (!root.IsMethod)
            {
                code.AppendLine(BlockFinish);
            }
        }

        if (root is { IsMethod: true, Source.BuilderRoots.IsDefaultOrEmpty: false })
        {
            code.AppendLine("#pragma warning restore CS0162");
        }
    }

    private static string GetTypeArguments(Root root)
    {
        var typeArgs = root.TypeDescription.TypeArgs;
        return typeArgs.Count == 0 ? "" : $"<{string.Join(", ", typeArgs)}>";
    }

    private void BuildTryRoot(CompositionCode composition, Root root)
    {
        var constraints = rootSignatureProvider.TryGetConstraints(composition, root);
        if (constraints is null)
        {
            return;
        }

        var code = composition.Code;
        code.AppendLine("#pragma warning disable CS0162");
        buildTools.AddNoInlining(code);
        code.AppendLine(GetTryRootSignature(composition, root));
        if (!constraints.IsEmpty)
        {
            using (code.Indent())
            {
                foreach (var constraint in constraints.OrderBy(i => i.Key.Name))
                {
                    code.AppendLine($"where {constraint.Key.Name}: {string.Join(", ", constraint.Value)}");
                }
            }
        }

        using (code.CreateBlock())
        {
            foreach (var arg in root.RootArgs.Where(i => i.InstanceType.IsReferenceType && i.InstanceType.NullableAnnotation != NullableAnnotation.Annotated))
            {
                code.AppendLine($"if ({buildTools.NullCheck(composition.Compilation, arg.Name)}) throw new {Names.SystemNamespace}ArgumentNullException(nameof({arg.Name}));");
            }

            code.AppendLine($"switch ({Names.BuildingInstance})");
            using (code.CreateBlock())
            {
                foreach (var builderRoot in root.Source.BuilderRoots)
                {
                    var rootType = typeResolver.Resolve(composition.Setup, builderRoot.RootType);
                    var instanceName = uniqueNameProvider.GetUniqueName($"{nameFormatter.Format("{type}", builderRoot.RootType, builderRoot.Tag?.Value)}");
                    code.AppendLine($"case {rootType} {instanceName}:");
                    using (code.Indent())
                    {
                        code.AppendLine($"{builderRoot.Name}({instanceName});");
                        code.AppendLine("return true;");
                    }
                }

                code.AppendLine("default:");
                using (code.Indent())
                {
                    code.AppendLine("return false;");
                }
            }

            code.AppendLine("return false;");
        }

        code.AppendLine("#pragma warning restore CS0162");
    }

    private string GetTryRootSignature(CompositionCode composition, Root root)
    {
        var rootSignature = new StringBuilder();
        rootSignature.Append(GetAccessModifier(root));
        if (root.IsStatic)
        {
            rootSignature.Append(" static");
        }

        if ((root.Kind & RootKinds.Partial) == RootKinds.Partial)
        {
            rootSignature.Append(" partial");
        }

        if ((root.Kind & RootKinds.Virtual) == RootKinds.Virtual)
        {
            rootSignature.Append(" virtual");
        }

        if ((root.Kind & RootKinds.Override) == RootKinds.Override)
        {
            rootSignature.Append(" override");
        }

        rootSignature.Append(" bool Try");
        rootSignature.Append(root.DisplayName);
        rootSignature.Append(GetTypeArguments(root));

        rootSignature.Append($"({string.Join(", ", root.RootArgs.Select(arg => $"{GetRootArgPrefix(arg)}{typeResolver.Resolve(composition.Setup, arg.InstanceType)} {arg.Name}"))})");
        return rootSignature.ToString();
    }

    private string GetAccessModifier(Root root) =>
        rootAccessModifierResolver.Resolve(root) switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "internal",
            Accessibility.Public => "public",
            _ => ""
        };

    private string GetRootArgPrefix(VarDeclaration arg) =>
        refSafety.IsMaybeRefLike(arg.InstanceType) ? "scoped " : "";

    private TypeDescription GetAttributeType(CompositionCode composition, Root root) =>
        marker.IsMarkerBased(composition.Setup, root.Injection.Type)
            ? new TypeDescription(root.Injection.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToString(), ImmutableArray<TypeDescription>.Empty, null)
            : typeResolver.ResolveRuntime(composition.Setup, root.Injection.Type);
}
