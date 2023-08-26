// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ArgFieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        var classArgs = composition.Args.Where(i => i.Node.Arg?.Source.Kind == ArgKind.Class).ToArray();
        if (!classArgs.Any())
        {
            return composition;
        }
        
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        foreach (var arg in classArgs)
        {
            code.AppendLine($"private readonly {arg.InstanceType} {arg.VarName};");
            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }
}