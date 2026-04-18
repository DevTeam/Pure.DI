// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core.Code.Parts;

sealed class ScopeConstructorBuilder(
    IConstructors constructors,
    ICodeNameProvider codeNameProvider)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.ScopeConstructor;

    public CompositionCode Build(CompositionCode composition)
    {
        if (!constructors.IsEnabled(composition, ConstructorKind.Scope))
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        string source, destination;
        if (composition.IsScopeMethod)
        {
            if (composition.Hints.IsCommentsEnabled)
            {
                code.AppendLine("/// <summary>");
                code.AppendLine($"/// This method setups <paramref name=\"{Names.ChildScopeArgName}\"/> scope based on <paramref name=\"{Names.ParentScopeArgName}\"/>. This allows the <see cref=\"Lifetime.Scoped\"/> life time to be applied.");
                code.AppendLine("/// </summary>");
                code.AppendLine($"/// <param name=\"{Names.ParentScopeArgName}\">Scope parent.</param>");
                code.AppendLine($"/// <param name=\"{Names.ChildScopeArgName}\">Scope child.</param>");
            }

            source = $"{Names.ParentScopeArgName}.";
            destination = $"{Names.ChildScopeArgName}.";
            code.AppendLine($"internal static {composition.Name.ClassName} {composition.ScopeMethodName}({composition.Name.ClassName} {Names.ParentScopeArgName}, {composition.Name.ClassName} {Names.ChildScopeArgName})");
        }
        else
        {
            if (composition.Hints.IsCommentsEnabled)
            {
                code.AppendLine("/// <summary>");
                code.AppendLine($"/// This constructor creates a new instance of <see cref=\"{composition.Name.ClassName}\"/> scope based on <paramref name=\"{Names.ParentScopeArgName}\"/>. This allows the <see cref=\"Lifetime.Scoped\"/> life time to be applied.");
                code.AppendLine("/// </summary>");
                code.AppendLine($"/// <param name=\"{Names.ParentScopeArgName}\">Scope parent.</param>");
            }

            source = $"{Names.ParentScopeArgName}.";
            destination = "";
            var ctorName = codeNameProvider.GetConstructorName(composition.Name.ClassName);
            code.AppendLine($"internal {ctorName}({composition.Name.ClassName} {Names.ParentScopeArgName})");
        }

        using (code.CreateBlock())
        {
            code.AppendLine($"if ({Names.ObjectTypeName}.ReferenceEquals({Names.ParentScopeArgName}, null)) throw new {Names.SystemNamespace}ArgumentNullException(nameof({Names.ParentScopeArgName}));");
            if (composition.IsScopeMethod)
            {
                code.AppendLine($"if ({Names.ObjectTypeName}.ReferenceEquals({Names.ChildScopeArgName}, null)) throw new {Names.SystemNamespace}ArgumentNullException(nameof({Names.ChildScopeArgName}));");
                if (composition.Singletons.Length > 0)
                {
                    code.AppendLine($"{destination}{Names.RootFieldName} = {Names.ParentScopeArgName}.{Names.RootFieldName} ?? {Names.ParentScopeArgName};");
                }
            }
            else
            {
                if (composition.Singletons.Length > 0)
                {
                    code.AppendLine($"{destination}{Names.RootFieldName} = {source}{Names.RootFieldName} ?? {Names.ParentScopeArgName};");
                }
            }

            if (!composition.IsScopeMethod)
            {
                foreach (var fieldArg in composition.ClassArgs.GetArgsOfKind(ArgKind.Composition))
                {
                    code.AppendLine($"{destination}{fieldArg.Name} = {source}{fieldArg.Name};");
                }
            }

            foreach (var contextArg in composition.SetupContextArgsToCopy)
            {
                if (composition.IsScopeMethod && contextArg.Kind == SetupContextKind.Argument)
                {
                    // Scope methods receive an already constructed child scope.
                    // Setup context arguments are stored in readonly fields and are initialized by that constructor.
                    continue;
                }

                code.AppendLine($"{destination}{contextArg.Name} = {source}{contextArg.Name};");
            }

            foreach (var memberName in composition.SetupContextMembersToCopy)
            {
                code.AppendLine($"{destination}{memberName} = {source}{memberName};");
            }

            if (composition.IsLockRequired)
            {
                code.AppendLine($"{destination}{Names.LockFieldName} = {source}{Names.LockFieldName};");
            }

            if (composition.DisposablesScopedCount > 0 && constructors.IsEnabled(composition.Source))
            {
                code.AppendLine($"{destination}{Names.DisposablesFieldName} = new object[{composition.DisposablesScopedCount.ToString()}];");
            }

            if (composition.IsScopeMethod)
            {
                code.AppendLine($"return {Names.ChildScopeArgName};");
            }
        }

        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}
