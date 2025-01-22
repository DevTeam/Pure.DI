// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

internal sealed class RootMethodsBuilder(
    IBuildTools buildTools,
    ITypeResolver typeResolver,
    [Tag(typeof(RootMethodsCommenter))] ICommenter<Root> rootCommenter,
    IMarker marker,
    IRootAccessModifierResolver rootAccessModifierResolver,
    CancellationToken cancellationToken)
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        if (composition.Roots.Length == 0)
        {
            return composition;
        }

        var code = composition.Code;
        var generatePrivateRoots = composition.Source.Source.Hints.IsResolveEnabled;
        var membersCounter = composition.MembersCount;
        code.AppendLine("#region Roots");
        var isFirst = true;
        foreach (var root in composition.Roots.Where(i => generatePrivateRoots || i.IsPublic))
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
        }

        code.AppendLine("#endregion");
        return composition with { MembersCount = membersCounter };
    }

    private void BuildRoot(CompositionCode composition, Root root)
    {
        rootCommenter.AddComments(composition, root);
        var code = composition.Code;
        var rootArgsStr = "";
        if (root.IsMethod)
        {
            rootArgsStr = $"({string.Join(", ", root.Args.Select(arg => $"{typeResolver.Resolve(composition.Source.Source, arg.InstanceType)} {arg.VariableDeclarationName}"))})";
            buildTools.AddPureHeader(code);
        }
        
        var name = new StringBuilder();
        var accessModifier = rootAccessModifierResolver.Resolve(root) switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "internal",
            Accessibility.Public => "public",
            _ => ""
        };

        name.Append(accessModifier);
        if ((root.Kind & RootKinds.Static) == RootKinds.Static)
        {
            name.Append(" static");
        }

        if ((root.Kind & RootKinds.Partial) == RootKinds.Partial)
        {
            name.Append(" partial");
        }

        name.Append(' ');
        name.Append(root.TypeDescription.Name);

        name.Append(' ');
        name.Append(root.DisplayName);

        var typeArgs = root.TypeDescription.TypeArgs;
        if (typeArgs.Count > 0)
        {
            name.Append('<');
            name.Append(string.Join(", ", typeArgs));
            name.Append('>');
        }

        name.Append(rootArgsStr);

        if ((root.Kind & RootKinds.Exposed) == RootKinds.Exposed)
        {
            var tag = root.Injection.Tag;
            if (tag == MdTag.ContextTag)
            {
                tag = null;
            }

            if (tag is not null)
            {
                code.AppendLine($"[{Names.BindAttributeName}(typeof({root.Injection.Type}), {Names.GeneratorName}.{nameof(Lifetime)}.{nameof(Lifetime.Transient)}, {tag.ValueToString()})]");
            }
            else
            {
                if (root.IsMethod && marker.IsMarkerBased(composition.Source.Source, root.Injection.Type))
                {
                    code.AppendLine($"[{Names.BindAttributeName}(typeof({root.Injection.Type}))]");
                }
                else
                {
                    code.AppendLine($"[{Names.BindAttributeName}]");
                }
            }
        }

        if (root.IsMethod)
        {
            code.AppendLine($"[{Names.MethodImplAttributeName}({Names.MethodImplAggressiveInlining})]");
        }

        code.AppendLine(name.ToString());

        if (typeArgs.Count > 0)
        {
            using (code.Indent())
            {
                foreach (var typeArg in typeArgs)
                {
                    if (typeArg.TypeParam is not { } typeParam)
                    {
                        continue;
                    }

                    var constrains = typeParam.ConstraintTypes.Select(i => typeResolver.Resolve(composition.Source.Source, i).Name).ToList();
                    if (typeParam.HasReferenceTypeConstraint)
                    {
                        constrains.Add("class");
                    }

                    if (typeParam.HasUnmanagedTypeConstraint)
                    {
                        constrains.Add("unmanaged");
                    }

                    if (typeParam.HasNotNullConstraint)
                    {
                        constrains.Add("notnull");
                    }

                    if (typeParam.HasValueTypeConstraint)
                    {
                        constrains.Add("struct");
                    }

                    if (typeParam.HasConstructorConstraint)
                    {
                        constrains.Add("new()");
                    }

                    if (constrains.Count == 0)
                    {
                        continue;
                    }

                    code.AppendLine($"where {typeArg.Name}: {string.Join(", ", constrains)}");
                }
            }
        }

        code.AppendLine("{");
        using (code.Indent())
        {
            var indentToken = Disposables.Empty;
            if (!root.IsMethod)
            {
                code.AppendLine($"[{Names.MethodImplAttributeName}({Names.MethodImplAggressiveInlining})]");
                code.AppendLine("get");
                code.AppendLine("{");
                indentToken = code.Indent();
            }

            try
            {
                if (composition.Source.Source.Hints.IsFormatCodeEnabled)
                {
                    var codeText = string.Join(Environment.NewLine, root.Lines);
                    var syntaxTree = CSharpSyntaxTree.ParseText(codeText, cancellationToken: cancellationToken);
                    foreach (var line in syntaxTree.GetRoot().NormalizeWhitespace("\t", "\n").GetText().Lines)
                    {
                        code.AppendLine(line.ToString());
                    }
                }
                else
                {
                    code.AppendLines(root.Lines);
                }
            }
            finally
            {
                indentToken.Dispose();
            }

            if (!root.IsMethod)
            {
                code.AppendLine("}");
            }
        }

        code.AppendLine("}");
    }
}