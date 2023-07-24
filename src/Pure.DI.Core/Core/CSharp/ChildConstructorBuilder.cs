// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.CSharp;

internal class ChildConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private const string ParentCompositionArgName = "parent";

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (membersCounter > 0)
        {
            code.AppendLine();
        }

        code.AppendLine($"internal {composition.Name.ClassName}({composition.Name.ClassName} {ParentCompositionArgName})");
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
                        cancellationToken.ThrowIfCancellationRequested();
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

        if (composition.Args.Any())
        {
            using (code.Indent())
            {
                foreach (var argsField in composition.Args)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    code.AppendLine($"{argsField.Name} = {ParentCompositionArgName}.{argsField.Name};");
                }
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}