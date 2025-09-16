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
        if (composition.ClassArgs.Length == 0)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.ClassArgs.Length == 0)
        {
            return composition;
        }

        constructorCommenter.AddComments(composition, Unit.Shared);

        code.AppendLine($"[{Names.OrdinalAttributeName}(128)]");
        var classArgs = composition.ClassArgs.GetArgsOfKind(ArgKind.Class).ToList();
        code.AppendLine($"public {composition.Source.Source.Name.ClassName}({string.Join(", ", classArgs.Select(arg => $"{typeResolver.Resolve(composition.Source.Source, arg.InstanceType)} {arg.Node.Arg?.Source.ArgName}"))})");
        using (code.CreateBlock())
        {
            foreach (var arg in classArgs)
            {
                var nullCheck = "";
                if (arg.InstanceType.IsReferenceType)
                {
                    nullCheck = $" ?? throw new {Names.SystemNamespace}ArgumentNullException(nameof({arg.Node.Arg?.Source.ArgName}))";
                }

                code.AppendLine($"{arg.Name} = {arg.Node.Arg?.Source.ArgName}{nullCheck};");
            }

            if (composition.Singletons.Length > 0)
            {
                code.AppendLine($"{Names.RootFieldName} = this;");
            }

            if (!composition.IsStaticThreadSafe && composition.IsLockRequired(locks))
            {
                code.AppendLine(new Line(int.MinValue, "#if NET9_0_OR_GREATER"));
                code.AppendLine($"{Names.LockFieldName} = new {Names.LockTypeName}();");
                code.AppendLine(new Line(int.MinValue, "#else"));
                code.AppendLine($"{Names.LockFieldName} = new {Names.ObjectTypeName}();");
                code.AppendLine(new Line(int.MinValue, "#endif"));
            }

            if (composition.TotalDisposablesCount > 0)
            {
                code.AppendLine($"{Names.DisposablesFieldName} = new object[{composition.TotalDisposablesCount.ToString()}];");
            }
        }

        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}