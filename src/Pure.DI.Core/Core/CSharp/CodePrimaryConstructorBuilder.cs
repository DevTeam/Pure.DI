namespace Pure.DI.Core.CSharp;

internal class CodePrimaryConstructorBuilder: IBuilder<ComposerCode, ComposerCode>
{
    public ComposerCode Build(ComposerCode composer, CancellationToken cancellationToken)
    {
        if (!composer.Args.Any())
        {
            return composer;
        }

        var code = composer.Code;
        var membersCounter = composer.MembersCount;
        if (!composer.Args.Any())
        {
            return composer;
        }

        if (membersCounter > 0)
        {
            code.AppendLine();
        }

        code.AppendLine($"public {composer.ClassName}({string.Join(", ", composer.Args.Select(i => $"{i.Node.Type} {i.Node.Arg?.Source.ArgName}"))})");
        code.AppendLine("{");
        using (code.Indent())
        {
            foreach (var arg in composer.Args)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (arg.Node.Type.IsValueType)
                {
                    continue;
                }

                code.AppendLine($"if (System.Object.ReferenceEquals({arg.Node.Arg?.Source.ArgName}, null))");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine($"throw new System.ArgumentNullException(\"{arg.Node.Arg?.Source.ArgName}\");");
                }

                code.AppendLine("}");
                code.AppendLine();
            }

            foreach (var arg in composer.Args)
            {
                cancellationToken.ThrowIfCancellationRequested();
                code.AppendLine($"{arg.Name} = {arg.Node.Arg?.Source.ArgName};");
            }
            
            if (composer.Singletons.Any())
            {
                code.AppendLine($"{Variable.DisposablesFieldName} = new {CodeConstants.DisposableTypeName}[{composer.DisposableSingletonsCount}];");
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composer with { MembersCount = membersCounter };
    }
}