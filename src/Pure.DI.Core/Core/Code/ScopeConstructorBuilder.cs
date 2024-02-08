// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ScopeConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private const string BaseCompositionArgName = "baseComposition";

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (membersCounter > 0)
        {
            code.AppendLine();
        }

        code.AppendLine("/// <summary>");
        code.AppendLine($"/// This constructor creates a new instance of <see cref=\"{composition.Source.Source.Name.ClassName}\"/> scope based on <paramref name=\"{BaseCompositionArgName}\"/>. This allows the <see cref=\"Lifetime.Scoped\"/> life time to be applied.");
        code.AppendLine("/// </summary>");
        code.AppendLine($"/// <param name=\"{BaseCompositionArgName}\">Base composition.</param>");
        code.AppendLine($"protected {composition.Source.Source.Name.ClassName}({composition.Source.Source.Name.ClassName} {BaseCompositionArgName})");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"{Names.ParentFieldName} = {BaseCompositionArgName}.{Names.ParentFieldName};");
            if (composition.IsThreadSafe)
            {
                code.AppendLine($"{Names.LockFieldName} = {Names.ParentFieldName}.{Names.LockFieldName};");
            }
            
            if (composition.DisposablesScopedCount > 0)
            {
                code.AppendLine($"{Names.DisposablesFieldName} = new {Names.IDisposableInterfaceName}[{composition.DisposablesScopedCount.ToString()}];");
            }
            
            var classArgs = composition.Args.Where(i => i.Node.Arg?.Source.Kind == ArgKind.Class).ToArray();
            if (classArgs.Any())
            {
                foreach (var argsField in classArgs)
                {
                    code.AppendLine($"{argsField.VariableName} = {BaseCompositionArgName}.{argsField.VariableName};");
                }
            }
        }
        
        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}