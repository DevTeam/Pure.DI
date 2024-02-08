// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class DefaultConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public DefaultConstructorBuilder()
    {
    }

    public CompositionCode Build(CompositionCode composition)
    {
        if (composition.Args.Any())
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (membersCounter > 0)
        {
            code.AppendLine();
        }

        code.AppendLine("/// <summary>");
        code.AppendLine($"/// This constructor creates a new instance of <see cref=\"{composition.Source.Source.Name.ClassName}\"/>.");
        code.AppendLine("/// </summary>");
        code.AppendLine($"public {composition.Source.Source.Name.ClassName}()");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"{Names.ParentFieldName} = this;");
            if (composition.IsThreadSafe)
            {
                code.AppendLine($"{Names.LockFieldName} = new object();");
            }
            
            if (composition.DisposablesCount > 0)
            {
                code.AppendLine($"{Names.DisposablesFieldName} = new {Names.IDisposableInterfaceName}[{composition.DisposablesCount.ToString()}];");
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}