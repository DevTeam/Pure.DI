// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.CSharp;

internal class RootPropertiesBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
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
            cancellationToken.ThrowIfCancellationRequested();
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                code.AppendLine();
            }

            BuildProperty(composition, root.Injection.Type, root);
            membersCounter++;
        }
        code.AppendLine("#endregion");
        return composition with { MembersCount = membersCounter };
    }
    
    private static void BuildProperty(CompositionCode composition, ITypeSymbol type, Root root)
    {
        var code = composition.Code;
        code.AppendLine($"{(root.IsPublic ? "public" : "private")} {type} {root.PropertyName}");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine(Constant.MethodImplOptions);
            code.AppendLine("get");
            code.AppendLine("{");
            using (code.Indent())
            {
                if (composition.Source.Source.Hints.GetHint(Hint.FormatCode) == SettingState.On)
                {
                    var codeText = string.Join(Environment.NewLine, root.Lines);
                    var syntaxTree = CSharpSyntaxTree.ParseText(codeText);
                    codeText = syntaxTree.GetRoot().NormalizeWhitespace().ToString();
                    var lines = codeText.Split(Environment.NewLine);
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

            code.AppendLine("}");
        }

        code.AppendLine("}");
    }
}