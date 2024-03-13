// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class RootMethodsBuilder(
    IBuildTools buildTools,
    ITypeResolver typeResolver,
    [Tag(typeof(RootMethodsCommenter))] ICommenter<Root> rootCommenter)
    : IBuilder<CompositionCode, CompositionCode>
{
    private static readonly string[] NewLineSeparators = [Environment.NewLine];

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

        var generatePrivateRoots = composition.Source.Source.Hints.IsResolveEnabled;
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

            BuildRoot(composition, root);
            membersCounter++;
        }
        code.AppendLine("#endregion");
        return composition with { MembersCount = membersCounter };
    }
    
    private void BuildRoot(CompositionCode composition, Root root)
    {
        var code = composition.Code;
        var rootArgsStr = "";
        if (root.IsMethod)
        {
            rootArgsStr = $"({string.Join(", ", root.Args.Select(arg => $"{typeResolver.Resolve(arg.InstanceType)} {arg.VariableName}"))})";
            buildTools.AddPureHeader(code);
        }
        
        var modifier = (root.Kind & RootKinds.Private) == RootKinds.Private 
            ? "private"
            : (root.Kind & RootKinds.Internal) == RootKinds.Internal
                ? "internal"
                : "public";

        var name = new StringBuilder();
        name.Append(modifier);
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
        name.Append(root.PropertyName);

        var typeArgs = root.TypeDescription.TypeArgs;
        if (typeArgs.Count > 0)
        {
            name.Append('<');
            name.Append(string.Join(", ", typeArgs));
            name.Append('>');
        }
        
        name.Append(rootArgsStr);

        rootCommenter.AddComments(composition, root);
        
        code.AppendLine(name.ToString());
        code.AppendLine("{");
        using (code.Indent())
        {
            var indentToken = Disposables.Empty;
            if (!root.IsMethod)
            {
                buildTools.AddPureHeader(code);
                code.AppendLine("get");
                code.AppendLine("{");
                indentToken = code.Indent();
            }

            try
            {
                if (composition.Source.Source.Hints.IsFormatCodeEnabled)
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

            if (!root.IsMethod)
            {
                code.AppendLine("}");
            }
        }

        code.AppendLine("}");
    }
}