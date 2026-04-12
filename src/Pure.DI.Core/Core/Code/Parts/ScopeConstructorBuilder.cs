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
        var hints = composition.Hints;
        var setupContextMembersToCopy = composition.SetupContextMembersToCopy;
        var classArgs = composition.ClassArgs.GetArgsOfKind(ArgKind.Composition).ToList();
        var setupContextArgsToCopy = composition.SetupContextArgsToCopy;
        var isLockRequired = composition.IsLockRequired;
        var requiresParentScope = composition.RequiresParentScope;
        var scopeFactoryName = composition.ScopeFactoryName;
        var isFactoryMethod = composition.IsFactoryMethod;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        string source, destination;
        if (isFactoryMethod)
        {
            if (isCommentsEnabled)
            {
                code.AppendLine("/// <summary>");
                code.AppendLine($"/// This method creates a new instance of <see cref=\"{composition.Name.ClassName}\"/> scope based on the current one. This allows the <see cref=\"Lifetime.Scoped\"/> life time to be applied.");
                code.AppendLine("/// </summary>");
            }

            source = "this.";
            destination = $"{Names.NewScopeVarName}.";
            code.AppendLine($"internal {composition.Name.ClassName} {scopeFactoryName}()");
        }
        else
        {
            if (isCommentsEnabled)
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
            if (isFactoryMethod)
            {
                var args = string.Join(", ", classArgs.Select(arg => $"{source}{arg.Name}"));
                code.AppendLine($"var {Names.NewScopeVarName} = new {composition.Name.ClassName}({args});");

                if (composition.Singletons.Length > 0)
                {
                    code.AppendLine($"{destination}{Names.RootFieldName} = this;");
                }
            }
            else
            {
                if (requiresParentScope)
                {
                    code.AppendLine($"if ({Names.ObjectTypeName}.ReferenceEquals({Names.ParentScopeArgName}, null)) throw new {Names.SystemNamespace}ArgumentNullException(nameof({Names.ParentScopeArgName}));");
                }

                if (composition.Singletons.Length > 0)
                {
                    code.AppendLine($"{destination}{Names.RootFieldName} = {source}{Names.RootFieldName};");
                }
            }

            if (!isFactoryMethod)
            {
                foreach (var fieldArg in classArgs)
                {
                    code.AppendLine($"{destination}{fieldArg.Name} = {source}{fieldArg.Name};");
                }
            }

            foreach (var contextArg in setupContextArgsToCopy)
            {
                code.AppendLine($"{destination}{contextArg.Name} = {source}{contextArg.Name};");
            }

            foreach (var memberName in setupContextMembersToCopy)
            {
                code.AppendLine($"{destination}{memberName} = {source}{memberName};");
            }

            if (isLockRequired)
            {
                code.AppendLine($"{destination}{Names.LockFieldName} = {source}{Names.LockFieldName};");
            }

            if (composition.DisposablesScopedCount > 0)
            {
                code.AppendLine($"{destination}{Names.DisposablesFieldName} = new object[{composition.DisposablesScopedCount.ToString()}];");
            }
            else
            {
                if (composition.TotalDisposablesCount > 0)
                {
                    code.AppendLine($"{destination}{Names.DisposablesFieldName} = {source}{Names.DisposablesFieldName};");
                }
            }

            if (isFactoryMethod)
            {
                code.AppendLine($"return {Names.NewScopeVarName};");
            }
        }

        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }
}
