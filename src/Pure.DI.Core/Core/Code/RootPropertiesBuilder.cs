// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class RootPropertiesBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private static readonly string[] NewLineSeparators = { Environment.NewLine };
    
    public CompositionCode Build(CompositionCode composition)
    {
        if (!composition.Roots.Any())
        {
            return composition;
        }
        
        var code = composition.Code;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        var generatePrivateRoots = composition.Source.Source.Hints.GetHint(Hint.Resolve, SettingState.On) == SettingState.On;
        var membersCounter = composition.MembersCount;
        code.AppendLine("#region Composition Roots");
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

            BuildRoot(composition, root.Injection.Type, root);
            membersCounter++;
        }
        code.AppendLine("#endregion");
        return composition with { MembersCount = membersCounter };
    }
    
    private static void BuildRoot(CompositionCode composition, ITypeSymbol type, Root root)
    {
        var code = composition.Code;
        var rootArgs = composition.Args.Where(i => i.Node.Arg?.Source.Kind == ArgKind.Root).ToArray();
        var hasRootArgs = false;
        var rootArgsStr = "";
        if (rootArgs.Any())
        {
            hasRootArgs = true;
            rootArgsStr = $"({string.Join(", ", rootArgs.Select(arg => $"{arg.InstanceType} {arg.Name}"))})";
        }

        if (hasRootArgs)
        {
            code.AppendLine(Constant.MethodImplOptions);
        }

        code.AppendLine($"{(root.IsPublic ? "public" : "private")} {type} {root.PropertyName}{rootArgsStr}");
        code.AppendLine("{");
        using (code.Indent())
        {
            var indentToken = Disposables.Empty;
            if (!hasRootArgs)
            {
                code.AppendLine(Constant.MethodImplOptions);
                code.AppendLine("get");
                code.AppendLine("{");
                indentToken = code.Indent();
            }

            try
            {
                if (composition.Source.Source.Hints.GetHint(Hint.FormatCode) == SettingState.On)
                {
                    var codeText = string.Join(Environment.NewLine, root.Lines);
                    var syntaxTree = CSharpSyntaxTree.ParseText(codeText);
                    codeText = syntaxTree.GetRoot().NormalizeWhitespace().ToString();
                    var lines = codeText.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        code.AppendLine(line);
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

            if (!hasRootArgs)
            {
                code.AppendLine("}");
            }
        }

        code.AppendLine("}");
    }
}