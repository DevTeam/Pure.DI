// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class PrimaryConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
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
        
        var classArgs = composition.Args.Where(arg => arg.Node.Arg?.Source.Kind == ArgKind.Class).ToArray();
        code.AppendLine($"public {composition.Source.Source.Name.ClassName}({string.Join(", ", classArgs.Select(arg => $"{arg.InstanceType} {arg.Node.Arg?.Source.ArgName}"))})");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"{Names.LockFieldName} = new object();");
            foreach (var arg in classArgs)
            {
                if (arg.InstanceType.IsValueType)
                {
                    continue;
                }

                code.AppendLine($"if ({Names.SystemNamespace}Object.ReferenceEquals({arg.Node.Arg?.Source.ArgName}, null))");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine($"throw new {Names.SystemNamespace}ArgumentNullException(\"{arg.Node.Arg?.Source.ArgName}\");");
                }

                code.AppendLine("}");
                code.AppendLine();
            }

            foreach (var arg in classArgs)
            {
                code.AppendLine($"{arg.VariableName} = {arg.Node.Arg?.Source.ArgName};");
            }

            if (composition.DisposableSingletonsCount > 0)
            {
                code.AppendLine($"{Names.DisposablesFieldName} = new {Names.IDisposableInterfaceName}[{composition.DisposableSingletonsCount.ToString()}];");
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}