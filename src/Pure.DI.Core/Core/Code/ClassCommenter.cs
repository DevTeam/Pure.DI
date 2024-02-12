namespace Pure.DI.Core.Code;

internal class ClassCommenter(
    IComments comments,
    IBuilder<IEnumerable<string>, Uri> mermaidUrlBuilder)
    : ICommenter<Unit>
{
    public void AddComments(CompositionCode composition, Unit unit)
    {
        var hints = composition.Source.Source.Hints;
        if (hints.GetHint(Hint.Comments, SettingState.On) != SettingState.On)
        {
            return;
        }
        
        var privateRootAdditionalComment = $"is a private composition root that can be resolved by methods like <see cref=\"{hints.GetValueOrDefault(Hint.ResolveMethodName, Names.ResolveMethodName)}{{T}}()\"/>.";
        var classComments = composition.Source.Source.Comments;
        var code = composition.Code;
        if (classComments.Count <= 0 && composition.Roots.Length <= 0)
        {
            return;
        }
        
        code.AppendLine("/// <summary>");
        if (classComments.Count > 0)
        {
            code.AppendLine("/// <para>");
            foreach (var comment in comments.Format(classComments))
            {
                code.AppendLine(comment);
            }

            code.AppendLine("/// </para>");
        }
                
        var orderedRoots = composition.Roots
            .OrderByDescending(root => root.IsPublic)
            .ThenBy(root => root.PropertyName)
            .ThenBy(root => root.Node.Binding)
            .ToArray();

        if (composition.Roots.Length > 0)
        {
            var rootComments = comments.FormatList(
                "Composition roots:",
                orderedRoots.Select(root => (CreateTerms(root), CreateDescriptions(root))));

            foreach (var rootComment in rootComments)
            {
                code.AppendLine(rootComment);
            }

            IReadOnlyCollection<string> CreateTerms(Root root) =>
                root.IsPublic
                    ? [$"<see cref=\"{root.Node.Type}\"/> {root.PropertyName}"]
                    : [$"<see cref=\"{root.Node.Type}\"/> {privateRootAdditionalComment}"];

            IReadOnlyCollection<string> CreateDescriptions(Root root) => root.Source.Comments;
        }

        var root = orderedRoots.FirstOrDefault(i => i.IsPublic);
        if (root is not null)
        {
            var classArgsStr = "()";
            if (!composition.Args.IsEmpty)
            {
                classArgsStr = $"({string.Join(", ", composition.Args.Select(arg => $"{arg.Node.Arg?.Source.ArgName ?? "..."}"))})";
            }

            var rootArgsStr = "";
            if (!root.Args.IsEmpty || (root.Kind & RootKinds.Method) == RootKinds.Method)
            {
                rootArgsStr = $"({string.Join(", ", root.Args.Select(arg => $"{arg.Node.Arg?.Source.ArgName ?? "..."}"))})";
            }

            var name = composition.Source.Source.Name;
            code.AppendLine("/// <example>");
            code.AppendLine($"/// This shows how to get an instance of type <see cref=\"{root.Node.Type}\"/> using the composition root <see cref=\"{root.PropertyName}\"/>:");
            code.AppendLine("/// <code>");
            code.AppendLine($"/// {(composition.DisposablesCount == 0 ? "" : "using ")}var composition = new {name.ClassName}{classArgsStr};");
            code.AppendLine($"/// var instance = composition.{root.PropertyName}{rootArgsStr};");
            code.AppendLine("/// </code>");
            code.AppendLine("/// </example>");
        }

        if (!composition.Diagram.IsEmpty)
        {
            var diagramUrl = mermaidUrlBuilder.Build(composition.Diagram.Select(i => i.Text));
            code.AppendLine($"/// <a href=\"{diagramUrl}\">Class diagram</a><br/>");
        }
        
        code.AppendLine("/// This class was created by <a href=\"https://github.com/DevTeam/Pure.DI\">Pure.DI</a> source code generator.");
        code.AppendLine("/// </summary>");
        code.AppendLine("/// <seealso cref=\"Pure.DI.DI.Setup\"/>");
    }
}