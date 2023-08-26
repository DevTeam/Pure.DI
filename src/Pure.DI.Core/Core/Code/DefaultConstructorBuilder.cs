// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class DefaultConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
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

        code.AppendLine($"public {composition.Source.Source.Name.ClassName}()");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"{Names.DisposablesFieldName} = new {Names.IDisposableInterfaceName}[{composition.DisposableSingletonsCount.ToString()}];");
        }

        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}