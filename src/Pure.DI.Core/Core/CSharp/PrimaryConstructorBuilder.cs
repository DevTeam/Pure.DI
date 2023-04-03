namespace Pure.DI.Core.CSharp;

internal class PrimaryConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        if (!composition.Args.Any())
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (!composition.Args.Any())
        {
            return composition;
        }

        if (membersCounter > 0)
        {
            code.AppendLine();
        }

        code.AppendLine($"public {composition.Name.ClassName}({string.Join(", ", composition.Args.Select(i => $"{i.InstanceType} {i.Node.Arg?.Source.ArgName}"))})");
        code.AppendLine("{");
        using (code.Indent())
        {
            foreach (var arg in composition.Args)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (arg.InstanceType.IsValueType)
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

            foreach (var arg in composition.Args)
            {
                cancellationToken.ThrowIfCancellationRequested();
                code.AppendLine($"{arg.Name} = {arg.Node.Arg?.Source.ArgName};");
            }
            
            if (composition.Singletons.Any())
            {
                code.AppendLine($"{Variable.DisposablesFieldName} = new {Constant.IDisposableInterfaceName}[{composition.DisposableSingletonsCount}];");
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}