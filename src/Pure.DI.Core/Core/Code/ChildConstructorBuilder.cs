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
            if (composition.Singletons.Any())
            {
                code.AppendLine($"lock ({ParentCompositionArgName}.{Names.LockFieldName})");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine($"{Names.LockFieldName} = {ParentCompositionArgName}.{Names.LockFieldName};");
                    
                    foreach (var singletonField in composition.Singletons)
                    {
                        code.AppendLine($"{singletonField.VariableName} = {ParentCompositionArgName}.{singletonField.VariableName};");

                        if (singletonField.InstanceType.IsValueType)
                        {
                            code.AppendLine($"{singletonField.VariableName}Created = {ParentCompositionArgName}.{singletonField.VariableName}Created;");
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
                    code.AppendLine($"{argsField.VariableName} = {ParentCompositionArgName}.{argsField.VariableName};");
                }
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}