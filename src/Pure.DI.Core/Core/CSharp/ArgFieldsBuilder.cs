namespace Pure.DI.Core.CSharp;

internal class ArgFieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        if (!composition.Args.Any())
        {
            return composition;
        }
        
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        foreach (var arg in composition.Args)
        {
            cancellationToken.ThrowIfCancellationRequested();
            code.AppendLine($"private readonly {arg.InstanceType} {arg.Name};");
            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }
}