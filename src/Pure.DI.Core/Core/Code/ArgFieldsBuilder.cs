// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ArgFieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        if (!composition.Args.Any())
        {
            return composition;
        }
        
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        foreach (var arg in composition.Args)
        {
            code.AppendLine($"private readonly {arg.InstanceType} {arg.Name};");
            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }
}