// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ChildConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private const string ParentCompositionArgName = "parent";

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (membersCounter > 0)
        {
            code.AppendLine();
        }

        code.AppendLine($"internal {composition.Source.Source.Name.ClassName}({composition.Source.Source.Name.ClassName} {ParentCompositionArgName})");
        code.AppendLine("{");
        using (code.Indent())
        {
            if (composition.DisposableSingletonsCount == 0)
            {
                code.AppendLine($"{Names.DisposablesFieldName} = new {Names.IDisposableInterfaceName}[0];");
            }

            if (composition.Singletons.Any())
            {
                code.AppendLine($"lock ({ParentCompositionArgName}.{Names.DisposablesFieldName})");
                code.AppendLine("{");
                using (code.Indent())
                {
                    if (composition.DisposableSingletonsCount > 0)
                    {
                        code.AppendLine($"{Names.DisposablesFieldName} = new {Names.IDisposableInterfaceName}[{composition.DisposableSingletonsCount.ToString()} - {ParentCompositionArgName}.{Names.DisposeIndexFieldName}];");
                    }

                    foreach (var singletonField in composition.Singletons)
                    {
                        code.AppendLine($"{singletonField.VarName} = {ParentCompositionArgName}.{singletonField.VarName};");

                        if (singletonField.InstanceType.IsValueType)
                        {
                            code.AppendLine($"{singletonField.VarName}Created = {ParentCompositionArgName}.{singletonField.VarName}Created;");
                        }
                    }
                }

                code.AppendLine("}");
            }
        }

        var classArgs = composition.Args.Where(i => i.Node.Arg?.Source.Kind == ArgKind.Class).ToArray();
        if (classArgs.Any())
        {
            using (code.Indent())
            {
                foreach (var argsField in classArgs)
                {
                    code.AppendLine($"{argsField.VarName} = {ParentCompositionArgName}.{argsField.VarName};");
                }
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}