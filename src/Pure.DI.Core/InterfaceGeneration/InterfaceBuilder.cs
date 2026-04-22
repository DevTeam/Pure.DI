namespace Pure.DI.InterfaceGeneration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

sealed class InterfaceBuilder(IRoslynSymbols roslynSymbols) : IInterfaceBuilder
{
    private static readonly SymbolDisplayFormat FullyQualifiedDisplayFormat = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeName,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    private static readonly SymbolDisplayFormat FullyQualifiedDisplayFormatForGrouping = new(
        genericsOptions: FullyQualifiedDisplayFormat.GenericsOptions,
        memberOptions: FullyQualifiedDisplayFormat.MemberOptions & ~SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: FullyQualifiedDisplayFormat.ParameterOptions,
        typeQualificationStyle: FullyQualifiedDisplayFormat.TypeQualificationStyle,
        globalNamespaceStyle: FullyQualifiedDisplayFormat.GlobalNamespaceStyle,
        miscellaneousOptions: FullyQualifiedDisplayFormat.MiscellaneousOptions);

    public string BuildInterfaceFor(ITypeSymbol typeSymbol, ClassDeclarationSyntax classSyntax)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return string.Empty;
        }

        var generationAttribute = typeSymbol.GetAttributes().FirstOrDefault(x =>
            x.AttributeClass != null && x.AttributeClass.Name.Contains(Names.GenerateInterfaceAttributeName, StringComparison.Ordinal));

        var symbolDetails = new GeneratedInterfaceDetails(generationAttribute, typeSymbol, classSyntax);
        var interfaceGenerator = new InterfaceCodeBuilder(symbolDetails.NamespaceName, symbolDetails.InterfaceName, symbolDetails.AccessLevel);

        interfaceGenerator.AddClassDocumentation(GetDocumentationForClass(classSyntax));
        interfaceGenerator.AddGeneric(GetGeneric(classSyntax, namedTypeSymbol));

        var members = roslynSymbols.GetAllMembers(typeSymbol)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => !x.IsStatic)
            .Where(x => !HasIgnoreAttribute(x))
            .ToList();

        AddPropertiesToInterface(members, interfaceGenerator);
        AddMethodsToInterface(members, interfaceGenerator);
        AddEventsToInterface(members, interfaceGenerator);

        return interfaceGenerator.Build();
    }

    private void AddMethodsToInterface(List<ISymbol> members, InterfaceCodeBuilder codeGenerator)
    {
        members.Where(x => x.Kind == SymbolKind.Method)
            .OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Ordinary)
            .Where(x => x.ContainingType.Name != nameof(Object))
            .Where(x => !HasIgnoreAttribute(x))
            .GroupBy(x => x.ToDisplayString(FullyQualifiedDisplayFormatForGrouping))
            .Select(g => g.First())
            .ToList()
            .ForEach(method => AddMethod(codeGenerator, method));
    }

    private void AddMethod(InterfaceCodeBuilder codeGenerator, IMethodSymbol method)
    {
        ActivateNullableIfNeeded(codeGenerator, method);

        var paramResult = new HashSet<string>();
        foreach (var parameter in method.Parameters.Select(p => GetParameterDisplayString(p, codeGenerator.HasNullable)))
        {
            paramResult.Add(parameter);
        }

        var typedArgs = method.TypeParameters
            .Select(arg => (arg.ToDisplayString(FullyQualifiedDisplayFormat), roslynSymbols.GetWhereStatement(arg, FullyQualifiedDisplayFormat)))
            .ToList();

        codeGenerator.AddMethodToInterface(
            method.Name,
            GetMethodReturnType(method),
            InheritDoc(method),
            paramResult,
            typedArgs);
    }

    private static string GetMethodReturnType(IMethodSymbol method)
    {
        var prefix = method.ReturnsByRefReadonly ? "ref readonly " : method.ReturnsByRef ? "ref " : string.Empty;
        return prefix + method.ReturnType.ToDisplayString(FullyQualifiedDisplayFormat);
    }

    private static void ActivateNullableIfNeeded(InterfaceCodeBuilder codeGenerator, ITypeSymbol typeSymbol)
    {
        if (IsNullable(typeSymbol))
        {
            codeGenerator.HasNullable = true;
        }
    }

    private static void ActivateNullableIfNeeded(InterfaceCodeBuilder codeGenerator, IMethodSymbol method)
    {
        if (method.Parameters.Any(x => IsNullable(x.Type)) || IsNullable(method.ReturnType))
        {
            codeGenerator.HasNullable = true;
        }
    }

    private static bool IsNullable(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        if (typeSymbol is not INamedTypeSymbol named)
        {
            return false;
        }

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var param in named.TypeArguments)
        {
            if (IsNullable(param))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetParameterDisplayString(IParameterSymbol param, bool nullableContextEnabled)
    {
        var paramParts = param.ToDisplayParts(FullyQualifiedDisplayFormat);
        var typeSb = new StringBuilder();
        var restSb = new StringBuilder();
        var isInsideType = true;
        foreach (var part in paramParts)
        {
            if (isInsideType && part.Kind == SymbolDisplayPartKind.Space)
            {
                isInsideType = false;
            }

            if (isInsideType)
            {
                typeSb.Append(part.ToString());
            }
            else
            {
                restSb.Append(part.ToString());
            }
        }

        if (param is { HasExplicitDefaultValue: true, ExplicitDefaultValue: null }
            && param.NullableAnnotation != NullableAnnotation.Annotated
            && param.Type.IsReferenceType
            && nullableContextEnabled)
        {
            typeSb.Append('?');
        }

        return typeSb.Append(restSb).ToString();
    }

    private static void AddEventsToInterface(List<ISymbol> members, InterfaceCodeBuilder codeGenerator)
    {
        members.Where(x => x.Kind == SymbolKind.Event)
            .OfType<IEventSymbol>()
            .GroupBy(x => x.ToDisplayString(FullyQualifiedDisplayFormatForGrouping))
            .Select(g => g.First())
            .ToList()
            .ForEach(evt =>
            {
                ActivateNullableIfNeeded(codeGenerator, evt.Type);
                codeGenerator.AddEventToInterface(evt.Name, evt.Type.ToDisplayString(FullyQualifiedDisplayFormat), InheritDoc(evt));
            });
    }

    private static void AddPropertiesToInterface(List<ISymbol> members, InterfaceCodeBuilder interfaceGenerator)
    {
        members.Where(x => x.Kind == SymbolKind.Property)
            .OfType<IPropertySymbol>()
            .Where(x => !x.IsIndexer)
            .GroupBy(x => x.Name)
            .Select(g => g.First())
            .ToList()
            .ForEach(prop =>
            {
                ActivateNullableIfNeeded(interfaceGenerator, prop.Type);

                interfaceGenerator.AddPropertyToInterface(
                    prop.Name,
                    prop.Type.ToDisplayString(FullyQualifiedDisplayFormat),
                    prop.GetMethod?.DeclaredAccessibility == Accessibility.Public,
                    GetSetKind(prop.SetMethod),
                    prop.ReturnsByRef,
                    InheritDoc(prop));
            });
    }

    private static PropertySetKind GetSetKind(IMethodSymbol? setMethodSymbol) =>
        setMethodSymbol switch
        {
            null => PropertySetKind.NoSet,
            { IsInitOnly: true, DeclaredAccessibility: Accessibility.Public } => PropertySetKind.Init,
            _ => setMethodSymbol is { DeclaredAccessibility: Accessibility.Public } ? PropertySetKind.Always : PropertySetKind.NoSet
        };

    private static bool HasIgnoreAttribute(ISymbol x) =>
        x.GetAttributes().Any(a => a.AttributeClass != null && a.AttributeClass.Name.Contains(Names.IgnoreInterfaceAttributeName, StringComparison.Ordinal));

    private static string GetDocumentationForClass(CSharpSyntaxNode classSyntax)
    {
        if (!classSyntax.HasLeadingTrivia)
        {
            return string.Empty;
        }

        SyntaxKind[] docSyntax =
        [
            SyntaxKind.DocumentationCommentExteriorTrivia,
            SyntaxKind.EndOfDocumentationCommentToken,
            SyntaxKind.MultiLineDocumentationCommentTrivia,
            SyntaxKind.SingleLineDocumentationCommentTrivia
        ];

        var trivia = classSyntax.GetLeadingTrivia()
            .Where(x => docSyntax.Contains(x.Kind()))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToFullString()));

        return trivia.ToFullString().Trim();
    }

    private string GetGeneric(TypeDeclarationSyntax classSyntax, INamedTypeSymbol typeSymbol)
    {
        var whereStatements = typeSymbol.TypeParameters
            .Select(typeParameter => roslynSymbols.GetWhereStatement(typeParameter, FullyQualifiedDisplayFormat))
            .Where(constraint => !string.IsNullOrEmpty(constraint));
        return $"{classSyntax.TypeParameterList?.ToFullString().Trim()} {string.Join(" ", whereStatements)}".Trim();
    }

    private static string InheritDoc(ISymbol source) =>
        $"/// <inheritdoc cref=\"{source.ToDisplayString().Replace("<", "{").Replace(">", "}").Replace("params ", string.Empty)}\" />";
}