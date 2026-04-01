// ReSharper disable ClassNeverInstantiated.Global

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
        var isCommentsEnabled = hints.IsCommentsEnabled;
        if (isCommentsEnabled)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine($"/// This constructor creates a new instance of <see cref=\"{composition.Source.Source.Name.ClassName}\"/> scope based on <paramref name=\"{Names.ParentScopeArgName}\"/>. This allows the <see cref=\"Lifetime.Scoped\"/> life time to be applied.");
            code.AppendLine("/// </summary>");
            code.AppendLine($"/// <param name=\"{Names.ParentScopeArgName}\">Scope parent.</param>");
        }

        var ctorName = codeNameProvider.GetConstructorName(composition.Source.Source.Name.ClassName);
        code.AppendLine($"internal {ctorName}({composition.Source.Source.Name.ClassName} {Names.ParentScopeArgName})");
        using (code.CreateBlock())
        {
            var parentScopeRef = Names.ParentScopeArgName;
            if (requiresParentScope)
            {
                const string parentScopeLocalName = "parentScopeChecked";
                code.AppendLine($"var {parentScopeLocalName} = {Names.ParentScopeArgName} ?? throw new {Names.SystemNamespace}ArgumentNullException(nameof({Names.ParentScopeArgName}));");
                parentScopeRef = parentScopeLocalName;
            }

            if (composition.Singletons.Length > 0)
            {
                code.AppendLine($"{Names.RootFieldName} = {parentScopeRef}.{Names.RootFieldName};");
            }

            foreach (var fieldArg in classArgs)
            {
                code.AppendLine($"{fieldArg.Name} = {parentScopeRef}.{fieldArg.Name};");
            }

            foreach (var contextArg in setupContextArgsToCopy)
            {
                code.AppendLine($"{contextArg.Name} = {parentScopeRef}.{contextArg.Name};");
            }

            foreach (var memberName in setupContextMembersToCopy)
            {
                code.AppendLine($"{memberName} = {parentScopeRef}.{memberName};");
            }

            if (isLockRequired)
            {
                code.AppendLine($"{Names.LockFieldName} = {parentScopeRef}.{Names.LockFieldName};");
            }

            if (composition.DisposablesScopedCount > 0)
            {
                code.AppendLine($"{Names.DisposablesFieldName} = new object[{composition.DisposablesScopedCount.ToString()}];");
            }
            else
            {
                if (composition.TotalDisposablesCount > 0)
                {
                    code.AppendLine($"{Names.DisposablesFieldName} = {parentScopeRef}.{Names.DisposablesFieldName};");
                }
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

        if (accessorList.Accessors.Any(accessor =>
                accessor.Kind() is SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration))
        {
            return true;
        }

        // Get-only auto-properties can be assigned in constructors.
        return accessorList.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null);
    }
}
