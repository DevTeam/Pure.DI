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

                if (composition.DisposableSingletonsCount == 0)
                {
                    code.AppendLine($"{Variable.DisposablesFieldName} = new {Constant.IDisposableInterfaceName}[0];");
                }

                code.AppendLine($"lock ({ParentCompositionArgName}.{Variable.DisposablesFieldName})");
                code.AppendLine("{");
                using (code.Indent())
                {
                    if (composition.DisposableSingletonsCount > 0)
                    {
                        code.AppendLine($"{Variable.DisposablesFieldName} = new {Constant.IDisposableInterfaceName}[{composition.DisposableSingletonsCount.ToString()} - {ParentCompositionArgName}.{Variable.DisposeIndexFieldName}];");
                    }

                    foreach (var singletonField in composition.Singletons)
                    {
                        code.AppendLine($"{singletonField.Name} = {ParentCompositionArgName}.{singletonField.Name};");

                        if (singletonField.InstanceType.IsValueType)
                        {
                            code.AppendLine($"{singletonField.Name}Created = {ParentCompositionArgName}.{singletonField.Name}Created;");
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
                    code.AppendLine($"{argsField.Name} = {ParentCompositionArgName}.{argsField.Name};");
                }
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}