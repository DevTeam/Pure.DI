// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core.Code.Parts;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

sealed class ScopeConstructorBuilder(
    ILocks locks,
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
        var hints = composition.Source.Source.Hints;
        var setupContextMembersToCopy = GetSetupContextMembersToCopy(composition);
        var classArgs = composition.ClassArgs.GetArgsOfKind(ArgKind.Composition).ToList();
        var setupContextArgsToCopy = composition.SetupContextArgs
            .Where(arg => arg.Kind != SetupContextKind.RootArgument)
            .ToList();
        var isLockRequired = composition.IsLockRequired(locks);
        var requiresParentScope = composition.Singletons.Length > 0
                                  || classArgs.Count > 0
                                  || setupContextArgsToCopy.Count > 0
                                  || setupContextMembersToCopy.Count > 0
                                  || isLockRequired
                                  || composition.TotalDisposablesCount > 0;
        var scopeFactoryName = hints.ScopeFactoryName;
        var isFactoryMethod = requiresParentScope && !string.IsNullOrWhiteSpace(scopeFactoryName);
        var isCommentsEnabled = hints.IsCommentsEnabled;
        string source, destination;
        if (isFactoryMethod)
        {
            if (isCommentsEnabled)
            {
                code.AppendLine("/// <summary>");
                code.AppendLine($"/// This method creates a new instance of <see cref=\"{composition.Source.Source.Name.ClassName}\"/> scope based on the current one. This allows the <see cref=\"Lifetime.Scoped\"/> life time to be applied.");
                code.AppendLine("/// </summary>");
            }

            source = "this.";
            destination = $"{Names.NewScopeVarName}.";
            code.AppendLine($"internal {composition.Source.Source.Name.ClassName} {scopeFactoryName}()");
        }
        else
        {
            if (isCommentsEnabled)
            {
                code.AppendLine("/// <summary>");
                code.AppendLine($"/// This constructor creates a new instance of <see cref=\"{composition.Source.Source.Name.ClassName}\"/> scope based on <paramref name=\"{Names.ParentScopeArgName}\"/>. This allows the <see cref=\"Lifetime.Scoped\"/> life time to be applied.");
                code.AppendLine("/// </summary>");
                code.AppendLine($"/// <param name=\"{Names.ParentScopeArgName}\">Scope parent.</param>");
            }

            source = $"{Names.ParentScopeArgName}.";
            destination = "";
            var ctorName = isFactoryMethod ? scopeFactoryName : codeNameProvider.GetConstructorName(composition.Source.Source.Name.ClassName);
            code.AppendLine($"internal {ctorName}({composition.Source.Source.Name.ClassName} {Names.ParentScopeArgName})");
        }

        using (code.CreateBlock())
        {
            if (isFactoryMethod)
            {
                var args = string.Join(", ", classArgs.Select(arg => $"{source}{arg.Name}"));
                code.AppendLine($"var {Names.NewScopeVarName} = new {composition.Source.Source.Name.ClassName}({args});");

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

    private static IReadOnlyCollection<string> GetSetupContextMembersToCopy(CompositionCode composition)
    {
        var memberNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var setupContextMembers in composition.SetupContextMembers)
        {
            foreach (var member in setupContextMembers.Members)
            {
                switch (member)
                {
                    case FieldDeclarationSyntax { Declaration: { } declaration }:
                        foreach (var variable in declaration.Variables)
                        {
                            if (!variable.Identifier.IsKind(SyntaxKind.None))
                            {
                                memberNames.Add(variable.Identifier.ValueText);
                            }
                        }

                        break;

                    case PropertyDeclarationSyntax property when IsPropertyAssignable(property):
                        if (!property.Identifier.IsKind(SyntaxKind.None))
                        {
                            memberNames.Add(property.Identifier.ValueText);
                        }

                        break;
                }
            }
        }

        return memberNames;
    }

    private static bool IsPropertyAssignable(PropertyDeclarationSyntax property)
    {
        if (property.ExpressionBody is not null || property.AccessorList is not { } accessorList)
        {
            return false;
        }

        return accessorList.Accessors.Any(accessor =>
               accessor.Kind() is SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration)
               || accessorList.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null);
    }
}
