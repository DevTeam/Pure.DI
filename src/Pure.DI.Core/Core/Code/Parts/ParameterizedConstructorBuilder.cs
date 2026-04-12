// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

sealed class ParameterizedConstructorBuilder(
    ITypeResolver typeResolver,
    [Tag(typeof(ParameterizedConstructorCommenter))] ICommenter<Unit> constructorCommenter,
    IConstructors constructors,
    ICodeNameProvider codeNameProvider)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.ParameterizedConstructor;

    public CompositionCode Build(CompositionCode composition)
    {
        if (!constructors.IsEnabled(composition, ConstructorKind.Parameterized))
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var setupContextArgs = composition.SetupContextArgs.Where(arg => arg.Kind == SetupContextKind.Argument).ToArray();
        if (composition.ClassArgs.Length == 0 && setupContextArgs.Length == 0)
        {
            return composition;
        }

        constructorCommenter.AddComments(composition, Unit.Shared);

        code.AppendLine($"[{Names.OrdinalAttributeName}(128)]");
        var classArgs = composition.ClassArgs.GetArgsOfKind(ArgKind.Composition).ToList();
        var ctorName = codeNameProvider.GetConstructorName(composition.Source.Source.Name.ClassName);
        var ctorArgs = classArgs
            .Select(arg => $"{typeResolver.Resolve(composition.Source.Source, arg.InstanceType)} {arg.Node.Arg?.Source.ArgName}")
            .Concat(setupContextArgs.Select(arg => $"{typeResolver.Resolve(composition.Source.Source, arg.Type)} {arg.Name}"));
        code.AppendLine($"public {ctorName}({string.Join(", ", ctorArgs)})");
        using (code.CreateBlock())
        {
            foreach (var arg in classArgs)
            {
                var nullCheck = "";
                if (arg.InstanceType.IsReferenceType)
                {
                    nullCheck = $" ?? throw new {Names.SystemNamespace}ArgumentNullException(nameof({arg.Node.Arg?.Source.ArgName}))";
                }

                var leftName = arg.Name == arg.Node.Arg?.Source.ArgName
                    ? $"this.{arg.Name}"
                    : arg.Name;

                code.AppendLine($"{leftName} = {arg.Node.Arg?.Source.ArgName}{nullCheck};");
            }

            foreach (var arg in setupContextArgs)
            {
                var nullCheck = "";
                if (arg.Type.IsReferenceType)
                {
                    nullCheck = $" ?? throw new {Names.SystemNamespace}ArgumentNullException(nameof({arg.Name}))";
                }

                code.AppendLine($"this.{arg.Name} = {arg.Name}{nullCheck};");
            }

            if (composition.Singletons.Length > 0)
            {
                code.AppendLine($"{Names.RootFieldName} = this;");
            }

            if (composition.IsLockRequired)
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
