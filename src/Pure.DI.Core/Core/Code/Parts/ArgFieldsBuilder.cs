// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

sealed class ArgFieldsBuilder(ITypeResolver typeResolver)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.ArgFields;

    public CompositionCode Build(CompositionCode composition)
    {
        var classArgs = composition.ClassArgs.GetArgsOfKind(ArgKind.Class).ToList();
        if (classArgs.Count == 0)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        foreach (var arg in classArgs)
        {
            code.AppendLine($"private readonly {typeResolver.Resolve(composition.Source.Source, arg.InstanceType)} {arg.Name};");
            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }
}