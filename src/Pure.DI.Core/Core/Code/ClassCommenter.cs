// ReSharper disable ConvertIfStatementToConditionalTernaryExpression

namespace Pure.DI.Core.Code;

sealed class ClassCommenter(
    IFormatter formatter,
    IComments comments,
    IBuilder<string, Uri> mermaidUrlBuilder,
    IBuilder<RootsContext, IEnumerable<ResolverInfo>> resolversBuilder,
    IInformation information)
    : ICommenter<Unit>
{
    public void AddComments(CompositionCode composition, Unit unit)
    {
        var hints = composition.Source.Source.Hints;
        if (!hints.IsCommentsEnabled)
        {
            return;
        }

        var classComments = composition.Source.Source.Comments;
        var code = composition.Code;
        if (classComments.Count <= 0 && composition.PublicRoots.Length <= 0)
        {
            return;
        }

        code.AppendLine("/// <summary>");
        try
        {
            if (composition.Diagram.Count > 0)
            {
                var diagramUrl = mermaidUrlBuilder.Build(composition.Diagram.ToString());
                code.AppendLine("/// <para>");
                code.AppendLine($"/// <a href=\"{diagramUrl}\">Class diagram</a>");
                code.AppendLine("/// </para>");
            }

            if (classComments.Count > 0)
            {
                code.AppendLine("/// <para>");
                foreach (var comment in comments.Format(classComments, true))
                {
                    code.AppendLine(comment);
                }

                code.AppendLine("/// </para>");
            }

            var orderedRoots = composition.PublicRoots
                .OrderByDescending(root => root.IsPublic)
                .ThenBy(root => root.DisplayName)
                .ThenBy(root => root.Node.Binding.Id)
                .ToList();

            if (orderedRoots.Count > 0)
            {
                var rootComments = comments.FormatList(
                    "<b>Composition roots:</b>",
                    orderedRoots.Select(root => (CreateRootTerms(root), CreateRootDescriptions(root))),
                    false);

                foreach (var rootComment in rootComments)
                {
                    code.AppendLine(rootComment);
                }

                IReadOnlyCollection<string> CreateRootTerms(Root root)
                {
                    var term = new StringBuilder();
                    if (root.IsPublic)
                    {
                        term.Append(formatter.FormatRef(root.Injection.Type));
                        term.Append(' ');
                        term.Append(formatter.FormatRef(composition.Source.Source, root));
                    }
                    else
                    {
                        term.Append("Anonymous composition root of type ");
                        term.Append(formatter.FormatRef(root.Injection.Type));
                        term.Append('.');
                    }

                    if (!hints.IsResolveEnabled)
                    {
                        return [term.ToString()];
                    }

                    var resolvers = resolversBuilder.Build(new RootsContext(composition.Source.Source, ImmutableArray.Create(root)));
                    if (!resolvers.Any())
                    {
                        return [term.ToString()];
                    }

                    if (root.IsPublic)
                    {
                        term.Append("<br/>or using ");
                    }
                    else
                    {
                        term.Append(" It can be resolved by ");
                    }

                    if (root.Injection.Tag is null)
                    {
                        term.Append(formatter.FormatRef($"{hints.ResolveMethodName}{{T}}()"));
                        term.Append(" method: <c>");
                        term.Append(hints.ResolveMethodName);
                    }
                    else
                    {
                        term.Append(formatter.FormatRef($"{hints.ResolveByTagMethodName}{{T}}(object)"));
                        term.Append(" method: <c>");
                        term.Append(hints.ResolveByTagMethodName);
                    }

                    term.Append("&lt;");
                    term.Append(comments.Escape(root.TypeDescription.Name.Replace(Names.GlobalNamespacePrefix, "")));
                    term.Append("&gt;(");
                    term.Append(root.Injection.Tag != null ? comments.Escape(root.Injection.Tag.ValueToString()) : "");
                    term.Append(")</c>");
                    return [term.ToString()];
                }

                IReadOnlyCollection<string> CreateRootDescriptions(Root root) =>
                    root.Source.Comments.Count > 0
                        ? root.Source.Comments.Select(comments.Escape).ToList()
                        : [$"Provides a composition root of type {formatter.FormatRef(root.Node.Type)}."];
            }

            var root = orderedRoots.FirstOrDefault(i => i.IsPublic);
            if (root is not null)
            {
                code.AppendLine("/// <example>");
                code.AppendLine($"/// This example shows how to get an instance of type {formatter.FormatRef(root.Node.Type)} using the composition root {formatter.FormatRef(composition.Source.Source, root)}:");
                code.AppendLine("/// <code>");
                code.AppendLine($"/// {(composition.TotalDisposablesCount == 0 ? "" : "using ")}var composition = new {composition.Source.Source.Name.ClassName}({string.Join(", ", composition.ClassArgs.Where(i => i.Node.Arg?.Source.Kind == ArgKind.Class).Select(arg => arg.Name))});");
                code.AppendLine($"/// var instance = composition.{formatter.Format(root)};");
                code.AppendLine("/// </code>");
                code.AppendLine("/// See also:");
                code.AppendLine("/// <br/><see cref=\"Pure.DI.DI.Setup\"/>");
                code.AppendLine("/// <br/><see cref=\"Pure.DI.IConfiguration.Bind(object[])\"/>");
                code.AppendLine("/// <br/><see cref=\"Pure.DI.IConfiguration.Bind{T}(object[])\"/>");
                code.AppendLine("/// </example>");
            }

            code.AppendLine("/// <para>");
            code.AppendLine($"/// This class was created by <a href=\"https://github.com/DevTeam/Pure.DI\">{information.ShortDescription}</a> source code generator.");
            code.AppendLine("/// </para>");
        }
        finally
        {
            code.AppendLine("/// </summary>");
        }
    }
}