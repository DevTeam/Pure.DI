// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.CSharp;

internal class DefaultConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
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

        code.AppendLine($"public {composition.Name.ClassName}()");
        code.AppendLine("{");
        using (code.Indent())
        {
            if (composition.Singletons.Any())
            {
                code.AppendLine($"{Variable.DisposablesFieldName} = new {Constant.IDisposableInterfaceName}[{composition.DisposableSingletonsCount.ToString()}];");
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}