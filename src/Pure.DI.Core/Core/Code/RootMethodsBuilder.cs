// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class RootMethodsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuildTools _buildTools;
    private static readonly string[] NewLineSeparators = { Environment.NewLine };

    public RootMethodsBuilder(IBuildTools buildTools) => 
        _buildTools = buildTools;

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
    
    private void BuildRoot(CompositionCode composition, ITypeSymbol type, Root root)
    {
        var code = composition.Code;
        var hasRootArgs = !root.Args.IsEmpty;
        var rootArgsStr = "";
        if (hasRootArgs)
        {
            hasRootArgs = true;
            rootArgsStr = $"({string.Join(", ", root.Args.Select(arg => $"{arg.InstanceType} {arg.VariableName}"))})";
        }

        if (hasRootArgs)
        {
            _buildTools.AddPureHeader(code);
        }

        code.AppendLine($"{(root.IsPublic ? "public" : "private")} {type} {root.PropertyName}{rootArgsStr}");
        code.AppendLine("{");
        using (code.Indent())
        {
            var indentToken = Disposables.Empty;
            if (!hasRootArgs)
            {
                _buildTools.AddPureHeader(code);
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