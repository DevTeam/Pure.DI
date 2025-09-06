// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

using static LinesExtensions;

sealed class RootMethodsBuilder(
    IBuildTools buildTools,
    IRootSignatureProvider rootSignatureProvider,
    [Tag(typeof(RootMethodsCommenter))] ICommenter<Root> rootCommenter,
    IMarker marker,
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
        var generatePrivateRoots = composition.Source.Source.Hints.IsResolveEnabled;
        var membersCounter = composition.MembersCount;
        code.AppendLine("#region Roots");
        var isFirst = true;
        foreach (var root in composition.PublicRoots.Where(i => generatePrivateRoots || i.IsPublic))
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
                foreach (var arg in root.RootArgs.Where(i => i.InstanceType.IsReferenceType))
                {
                    code.AppendLine($"if ({buildTools.NullCheck(composition.Source.Source.SemanticModel.Compilation, arg.Name)}) throw new {Names.SystemNamespace}ArgumentNullException(nameof({arg.Name}));");
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
                if (composition.Source.Source.Hints.IsFormatCodeEnabled)
                {
                    var codeText = string.Join(Environment.NewLine, root.Lines);
                    var syntaxTree = CSharpSyntaxTree.ParseText(codeText, cancellationToken: cancellationToken);
                    foreach (var line in syntaxTree.GetRoot().NormalizeWhitespace("\t", Environment.NewLine).GetText().Lines)
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
                code.AppendLine(BlockFinish);
            }
        }

        if (root is { IsMethod: true, Source.BuilderRoots.IsDefaultOrEmpty: false })
        {
            code.AppendLine("#pragma warning restore CS0162");
        }
    }
}