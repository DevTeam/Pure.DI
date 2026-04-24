// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.InterfaceGeneration;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

sealed class InterfaceBuilder(
    IRoslynSymbols roslynSymbols,
    ITypes types,
    Func<IBuilder<GeneratedInterfaceDetails, Lines>> interfaceCodeBuilderFactory)
    : IInterfaceBuilder
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

    public Lines BuildInterfaceFor(SemanticModel semanticModel, ITypeSymbol typeSymbol, ClassDeclarationSyntax classSyntax)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return new Lines();
        }

        var generationAttribute = typeSymbol.GetAttributes().FirstOrDefault(x =>
            x.AttributeClass != null && x.AttributeClass.Name.Contains(Names.GenerateInterfaceAttributeName, StringComparison.Ordinal));

        var symbolDetails = new GeneratedInterfaceDetails(semanticModel, generationAttribute, typeSymbol, classSyntax)
        {
            ClassDocumentation = GetDocumentationForClass(classSyntax),
            GenericType = GetGeneric(classSyntax, namedTypeSymbol)
        };

        var members = roslynSymbols.GetAllMembers(typeSymbol)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => !x.IsStatic)
            .Where(x => !HasIgnoreAttribute(x))
            .ToList();

        var hasNullable = false;
        symbolDetails.PropertyInfos = GetProperties(members, ref hasNullable);
        symbolDetails.MethodInfos = GetMethods(semanticModel, members, ref hasNullable);
        symbolDetails.Events = GetEvents(members, ref hasNullable);

        return interfaceCodeBuilderFactory().Build(symbolDetails);
    }

    private ImmutableArray<MethodInfo> GetMethods(SemanticModel semanticModel, List<ISymbol> members, ref bool hasNullable)
    {
        var methods = new List<MethodInfo>();
        foreach (var method in members.Where(x => x.Kind == SymbolKind.Method)
                     .OfType<IMethodSymbol>()
                     .Where(x => x.MethodKind is MethodKind.Ordinary)
                     .Where(x => !types.TypeEquals(x.ContainingType, semanticModel.Compilation.GetSpecialType(SpecialType.System_Object)))
                     .Where(x => !HasIgnoreAttribute(x))
                     .GroupBy(x => x.ToDisplayString(FullyQualifiedDisplayFormatForGrouping))
                     .Select(g => g.First()))
        {
            methods.Add(GetMethodInfo(method, ref hasNullable));
        }

        return methods.ToImmutableArray();
    }

    private MethodInfo GetMethodInfo(IMethodSymbol method, ref bool hasNullable)
    {
        ActivateNullableIfNeeded(ref hasNullable, method);

        var paramResult = new HashSet<string>();
        foreach (var methodParameter in method.Parameters)
        {
            var parameter = GetParameterDisplayString(methodParameter, hasNullable);
            paramResult.Add(parameter);
        }

        var typedArgs = method.TypeParameters
            .Select(arg => (arg.ToDisplayString(FullyQualifiedDisplayFormat), roslynSymbols.GetWhereStatement(arg, FullyQualifiedDisplayFormat)))
            .ToList();

        return new MethodInfo(
            method.Name,
            GetMethodReturnType(method),
            InheritDoc(method),
            paramResult.ToImmutableArray(),
            typedArgs.ToImmutableArray());
    }

    private static string GetMethodReturnType(IMethodSymbol method)
    {
        var prefix = method.ReturnsByRefReadonly ? "ref readonly " : method.ReturnsByRef ? "ref " : string.Empty;
        return prefix + method.ReturnType.ToDisplayString(FullyQualifiedDisplayFormat);
    }

    private static void ActivateNullableIfNeeded(ref bool hasNullable, ITypeSymbol typeSymbol)
    {
        if (IsNullable(typeSymbol))
        {
            hasNullable = true;
        }
    }

    private static void ActivateNullableIfNeeded(ref bool hasNullable, IMethodSymbol method)
    {
        if (method.Parameters.Any(x => IsNullable(x.Type)) || IsNullable(method.ReturnType))
        {
            hasNullable = true;
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

    private static ImmutableArray<EventInfo> GetEvents(List<ISymbol> members, ref bool hasNullable)
    {
        var events = new List<EventInfo>();
        foreach (var evt in members.Where(x => x.Kind == SymbolKind.Event)
                     .OfType<IEventSymbol>()
                     .GroupBy(x => x.ToDisplayString(FullyQualifiedDisplayFormatForGrouping))
                     .Select(g => g.First()))
        {
            ActivateNullableIfNeeded(ref hasNullable, evt.Type);
            events.Add(new EventInfo(evt.Name, evt.Type.ToDisplayString(FullyQualifiedDisplayFormat), InheritDoc(evt)));
        }

        return events.ToImmutableArray();
    }

    private static ImmutableArray<PropertyInfo> GetProperties(List<ISymbol> members, ref bool hasNullable)
    {
        var properties = new List<PropertyInfo>();
        foreach (var prop in members.Where(x => x.Kind == SymbolKind.Property)
                     .OfType<IPropertySymbol>()
                     .Where(x => !x.IsIndexer)
                     .GroupBy(x => x.Name)
                     .Select(g => g.First()))
        {
            ActivateNullableIfNeeded(ref hasNullable, prop.Type);

            properties.Add(new PropertyInfo(
                prop.Name,
                prop.Type.ToDisplayString(FullyQualifiedDisplayFormat),
                prop.GetMethod?.DeclaredAccessibility == Accessibility.Public,
                GetSetKind(prop.SetMethod),
                prop.ReturnsByRef,
                InheritDoc(prop)));
        }

        return properties.ToImmutableArray();
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
