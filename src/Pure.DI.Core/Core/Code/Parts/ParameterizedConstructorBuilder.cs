// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

sealed class ParameterizedConstructorBuilder(
    ITypeResolver typeResolver,
    [Tag(typeof(ParameterizedConstructorCommenter))] ICommenter<Unit> constructorCommenter,
    ILocks locks)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.ParameterizedConstructor;

    public CompositionCode Build(CompositionCode composition)
    {
        if (composition.Args.Length == 0)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.Args.Length == 0)
        {
            return composition;
        }

        constructorCommenter.AddComments(composition, Unit.Shared);

        code.AppendLine($"[{Names.OrdinalAttributeName}(128)]");
        var classArgs = composition.Args.GetArgsOfKind(ArgKind.Class).ToList();
        code.AppendLine($"public {composition.Source.Source.Name.ClassName}({string.Join(", ", classArgs.Select(arg => $"{typeResolver.Resolve(composition.Source.Source, arg.InstanceType)} {arg.Node.Arg?.Source.ArgName}"))})");
        code.AppendLine("{");
        using (code.Indent())
        {
            foreach (var arg in classArgs)
            {
                var nullCheck = "";
                if (arg.InstanceType.IsReferenceType)
                {
                    nullCheck = $" ?? throw new {Names.SystemNamespace}ArgumentNullException(nameof({arg.Node.Arg?.Source.ArgName}))";
                }

                code.AppendLine($"{arg.VariableDeclarationName} = {arg.Node.Arg?.Source.ArgName}{nullCheck};");
            }

            code.AppendLine($"{Names.RootFieldName} = this;");
            if (composition.IsThreadSafe)
            {
                code.AppendLine($"{Names.LockFieldName} = new {locks.GetLockType(composition.Source.Source)}();");
            }

            if (composition.TotalDisposablesCount > 0)
            {
                code.AppendLine($"{Names.DisposablesFieldName} = new object[{composition.TotalDisposablesCount.ToString()}];");
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}