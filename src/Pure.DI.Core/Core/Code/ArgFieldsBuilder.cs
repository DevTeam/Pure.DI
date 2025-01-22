// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

internal sealed class ArgFieldsBuilder(ITypeResolver typeResolver)
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        var classArgs = composition.Args.GetArgsOfKind(ArgKind.Class).ToList();
        if (classArgs.Count == 0)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        foreach (var arg in classArgs)
        {
            code.AppendLine($"private readonly {typeResolver.Resolve(composition.Source.Source, arg.InstanceType)} {arg.VariableDeclarationName};");
            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }
}