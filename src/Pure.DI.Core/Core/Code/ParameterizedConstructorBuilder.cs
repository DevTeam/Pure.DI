// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ParameterizedConstructorBuilder(
    ITypeResolver typeResolver,
    [Tag(typeof(ParameterizedConstructorCommenter))] ICommenter<Unit> constructorCommenter)
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        if (!composition.Args.Any())
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (!composition.Args.Any())
        {
            return composition;
        }

        constructorCommenter.AddComments(composition, Unit.Shared);
        
        var classArgs = composition.Args.GetArgsOfKind(ArgKind.Class).ToArray();
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
                code.AppendLine($"{Names.LockFieldName} = new object();");
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