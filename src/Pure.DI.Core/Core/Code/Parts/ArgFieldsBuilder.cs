// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

sealed class ArgFieldsBuilder(ITypeResolver typeResolver)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.ArgFields;

    public CompositionCode Build(CompositionCode composition)
    {
        var classArgs = composition.ClassArgs.GetArgsOfKind(ArgKind.Composition).ToList();
        if (classArgs.Count == 0 && composition.SetupContextArgs.Length == 0)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        foreach (var arg in classArgs)
        {
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private readonly {typeResolver.Resolve(composition.Setup, arg.InstanceType)} {arg.Name};");
            membersCounter++;
        }

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var arg in composition.SetupContextArgs)
        {
            if (classArgs.Any(existing => existing.Name == arg.Name))
            {
                continue;
            }

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (arg.Kind)
            {
                case SetupContextKind.Argument:
                    var typeName = typeResolver.Resolve(composition.Setup, arg.Type);
                    code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private readonly {typeName} {arg.Name};");
                    membersCounter++;
                    break;
            }
        }

        return composition with { MembersCount = membersCounter };
    }
}
