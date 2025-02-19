// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

sealed class ScopeConstructorBuilder : IClassPartBuilder
{
    public ClassPart Part => ClassPart.ScopeConstructor;

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var hints = composition.Source.Source.Hints;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        if (isCommentsEnabled)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine($"/// This constructor creates a new instance of <see cref=\"{composition.Source.Source.Name.ClassName}\"/> scope based on <paramref name=\"{Names.ParentScopeArgName}\"/>. This allows the <see cref=\"Lifetime.Scoped\"/> life time to be applied.");
            code.AppendLine("/// </summary>");
            code.AppendLine($"/// <param name=\"{Names.ParentScopeArgName}\">Scope parent.</param>");
        }

        code.AppendLine($"internal {composition.Source.Source.Name.ClassName}({composition.Source.Source.Name.ClassName} {Names.ParentScopeArgName})");
        using (code.CreateBlock())
        {
            code.AppendLine($"{Names.RootFieldName} = ({Names.ParentScopeArgName} ?? throw new {Names.SystemNamespace}ArgumentNullException(nameof({Names.ParentScopeArgName}))).{Names.RootFieldName};");
            var classArgs = composition.Args.GetArgsOfKind(ArgKind.Class).ToList();
            if (classArgs.Count > 0)
            {
                foreach (var argsField in classArgs)
                {
                    code.AppendLine($"{argsField.VariableDeclarationName} = {Names.RootFieldName}.{argsField.VariableDeclarationName};");
                }
            }

            if (composition.IsThreadSafe)
            {
                code.AppendLine($"{Names.LockFieldName} = {Names.RootFieldName}.{Names.LockFieldName};");
            }

            if (composition.DisposablesScopedCount > 0)
            {
                code.AppendLine($"{Names.DisposablesFieldName} = new object[{composition.DisposablesScopedCount.ToString()}];");
            }
            else
            {
                if (composition.TotalDisposablesCount > 0)
                {
                    code.AppendLine($"{Names.DisposablesFieldName} = {Names.ParentScopeArgName}.{Names.DisposablesFieldName};");
                }
            }
        }

        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}