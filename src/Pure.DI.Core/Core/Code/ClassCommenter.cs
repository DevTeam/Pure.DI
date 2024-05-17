// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
namespace Pure.DI.Core.Code;

internal class ClassCommenter(
    IFormatter formatter,
    IComments comments,
    IBuilder<IEnumerable<string>, Uri> mermaidUrlBuilder,
    IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
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
        if (classComments.Count <= 0 && composition.Roots.Length <= 0)
        {
            return;
        }
        
        code.AppendLine("/// <summary>");
        if (classComments.Count > 0)
        {
            foreach (var comment in comments.Format(classComments, true))
            {
                code.AppendLine(comment);
            }
        }
                
        var orderedRoots = composition.Roots
            .OrderByDescending(root => root.IsPublic)
            .ThenBy(root => root.DisplayName)
            .ThenBy(root => root.Node.Binding)
            .ToArray();

        if (orderedRoots.Length > 0)
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
                    term.Append(formatter.FormatRef(root));
                }
                else
                {
                    term.Append("Private composition root of type ");
                    term.Append(formatter.FormatRef(root.Injection.Type));
                    term.Append('.');
                }
                
                var resolvers = resolversBuilder.Build(ImmutableArray.Create(root));
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
                    term.Append("&lt;");
                }
                else
                {
                    term.Append(formatter.FormatRef($"{hints.ResolveByTagMethodName}{{T}}(object)"));
                    term.Append(" method: <c>");
                    term.Append(hints.ResolveByTagMethodName);
                    term.Append("&lt;");
                }
                
                term.Append(comments.Escape(root.TypeDescription.Name));
                term.Append("&gt;(");
                term.Append(root.Injection.Tag != null ? root.Injection.Tag.ValueToString() : "");
                term.Append(")</c>");
                return [term.ToString()];
            }

            IReadOnlyCollection<string> CreateRootDescriptions(Root root) => 
                root.Source.Comments.Count > 0 
                    ? root.Source.Comments.Select(comments.Escape).ToList() 
                    : [ $"Provides a composition root of type {formatter.FormatRef(root.Node.Type)}." ];
        }
        
        var root = orderedRoots.FirstOrDefault(i => i.IsPublic);
        if (root is not null)
        {
            code.AppendLine("/// <example>");
            code.AppendLine($"/// This example shows how to get an instance of type {formatter.FormatRef(root.Node.Type)} using the composition root {formatter.FormatRef(root)}:");
            code.AppendLine("/// <code>");
            code.AppendLine($"/// {(composition.TotalDisposablesCount == 0 ? "" : "using ")}var composition = new {composition.Source.Source.Name.ClassName}({string.Join(", ", composition.Args.Where(i => i.Node.Arg?.Source.Kind == ArgKind.Class).Select(arg => arg.VariableDeclarationName))});");
            code.AppendLine($"/// var instance = composition.{formatter.Format(root)};");
            code.AppendLine("/// </code>");
            code.AppendLine("/// See also:");
            code.AppendLine("/// <br/><see cref=\"Pure.DI.DI.Setup\"/>");
            code.AppendLine("/// <br/><see cref=\"Pure.DI.IConfiguration.Bind(object[])\"/>");
            code.AppendLine("/// <br/><see cref=\"Pure.DI.IConfiguration.Bind{T}(object[])\"/>");
            code.AppendLine("/// </example>");
        }

        code.AppendLine("/// <br/>");
        if (!composition.Diagram.IsEmpty)
        {
            var diagramUrl = mermaidUrlBuilder.Build(composition.Diagram.Select(i => i.Text));
            code.AppendLine($"/// <br/><a href=\"{diagramUrl}\">Class diagram</a><br/>");
        }
        
        code.AppendLine("/// <br/>This class was created by <a href=\"https://github.com/DevTeam/Pure.DI\">Pure.DI</a> source code generator.");
        code.AppendLine("/// </summary>");
    }
}