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
    IArguments arguments,
    ILocationProvider locationProvider,
    Func<IBuilder<GeneratedInterfaceDetails, Lines>> interfaceCodeBuilderFactory)
    : IInterfaceBuilder
{
    private readonly record struct InterfaceKey(string NamespaceName, string InterfaceName);

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

    public GeneratedInterfacesResult BuildInterfacesFor(
        SemanticModel semanticModel,
        ITypeSymbol typeSymbol,
        ClassDeclarationSyntax classSyntax)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return new GeneratedInterfacesResult(
                ImmutableArray<GeneratedInterfaceSource>.Empty,
                ImmutableArray<GeneratedInterfaceWarning>.Empty);
        }

        const string generateInterfaceAttributeFullName = $"{Names.GlobalNamespacePrefix}{Names.GenerateInterfaceAttributeFullName}";
        var defaultNamespaceName = GetNamespaceName(typeSymbol.ContainingNamespace);
        var defaultInterfaceName = $"I{classSyntax.Identifier.Text}";

        var settingsByInterface = new Dictionary<InterfaceKey, (bool AsInternal, bool HasClassAttribute)>();
        var selectiveInterfaces = new HashSet<InterfaceKey>();
        var selectedMembersByInterface = new Dictionary<InterfaceKey, HashSet<ISymbol>>();
        var selectiveInterfaceLocations = new Dictionary<InterfaceKey, Location>();
        var warnings = new List<GeneratedInterfaceWarning>();

        var classGenerateAttributes = typeSymbol.GetAttributes()
            .Where(x => x.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == generateInterfaceAttributeFullName)
            .ToList();

        foreach (var classGenerateAttribute in classGenerateAttributes)
        {
            var settings = GetInterfaceGenerationSettings(
                semanticModel.Compilation,
                classGenerateAttribute,
                defaultNamespaceName,
                defaultInterfaceName,
                false);
            var key = new InterfaceKey(settings.namespaceName, settings.interfaceName);
            if (!settingsByInterface.TryGetValue(key, out var existing))
            {
                settingsByInterface[key] = (settings.asInternal, true);
                continue;
            }

            settingsByInterface[key] = (existing.AsInternal, true);
        }

        var allMembers = roslynSymbols.GetAllMembers(typeSymbol)
            .Where(x => x.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event)
            .ToList();
        var eligibleMembers = allMembers
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => !x.IsStatic)
            .ToList();

        foreach (var member in allMembers)
        {
            var generationAttributes = member.GetAttributes()
                .Where(x => x.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == generateInterfaceAttributeFullName)
                .ToList();
            if (generationAttributes.Count == 0)
            {
                continue;
            }

            foreach (var generationAttribute in generationAttributes)
            {
                var settings = GetInterfaceGenerationSettings(
                    semanticModel.Compilation,
                    generationAttribute,
                    defaultNamespaceName,
                    defaultInterfaceName,
                    false);
                var key = new InterfaceKey(settings.namespaceName, settings.interfaceName);
                selectiveInterfaces.Add(key);
                if (!selectiveInterfaceLocations.ContainsKey(key))
                {
                    selectiveInterfaceLocations.Add(key, GetLocation(member, generationAttribute));
                }

                if (!settingsByInterface.TryGetValue(key, out _))
                {
                    settingsByInterface[key] = (settings.asInternal, false);
                }

                var memberName = member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                var interfaceDisplayName = GetInterfaceDisplayName(key);
                if (member.IsStatic)
                {
                    warnings.Add(new GeneratedInterfaceWarning(
                        LogId.WarningGenerateInterfaceOnStaticMember,
                        string.Format(Strings.Warning_Template_GenerateInterfaceOnStaticMember, memberName, interfaceDisplayName),
                        nameof(Strings.Warning_Template_GenerateInterfaceOnStaticMember),
                        GetLocation(member, generationAttribute)));
                    continue;
                }

                if (member.DeclaredAccessibility != Accessibility.Public)
                {
                    warnings.Add(new GeneratedInterfaceWarning(
                        LogId.WarningGenerateInterfaceOnNonPublicMember,
                        string.Format(Strings.Warning_Template_GenerateInterfaceOnNonPublicMember, memberName, interfaceDisplayName),
                        nameof(Strings.Warning_Template_GenerateInterfaceOnNonPublicMember),
                        GetLocation(member, generationAttribute)));
                    continue;
                }

                if (HasIgnoreAttribute(member))
                {
                    continue;
                }

                if (!selectedMembersByInterface.TryGetValue(key, out var selectedMembers))
                {
                    selectedMembers = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                    selectedMembersByInterface.Add(key, selectedMembers);
                }

                selectedMembers.Add(member);
            }
        }

        if (settingsByInterface.Count == 0)
        {
            return new GeneratedInterfacesResult(
                ImmutableArray<GeneratedInterfaceSource>.Empty,
                warnings.ToImmutableArray());
        }

        var nullableContextEnabled = semanticModel.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable;
        var generatedSources = ImmutableArray.CreateBuilder<GeneratedInterfaceSource>();
        foreach (var pair in settingsByInterface)
        {
            var key = pair.Key;
            var settings = pair.Value;
            var interfaceMembers = selectiveInterfaces.Contains(key)
                ? selectedMembersByInterface.TryGetValue(key, out var selectedMembers)
                    ? eligibleMembers.Where(selectedMembers.Contains).ToList()
                    : []
                : settings.HasClassAttribute
                    ? eligibleMembers.Where(x => !HasIgnoreAttribute(x)).ToList()
                    : [];
            if (selectiveInterfaces.Contains(key)
                && interfaceMembers.Count == 0
                && selectiveInterfaceLocations.TryGetValue(key, out var selectiveLocation))
            {
                warnings.Add(new GeneratedInterfaceWarning(
                    LogId.WarningGenerateInterfaceSelectiveEmpty,
                    string.Format(Strings.Warning_Template_GenerateInterfaceSelectiveEmpty, GetInterfaceDisplayName(key)),
                    nameof(Strings.Warning_Template_GenerateInterfaceSelectiveEmpty),
                    selectiveLocation));
            }

            var symbolDetails = new GeneratedInterfaceDetails(semanticModel, key.NamespaceName, key.InterfaceName, settings.AsInternal)
            {
                ClassDocumentation = GetDocumentationForClass(classSyntax),
                GenericType = GetGeneric(classSyntax, namedTypeSymbol),
                PropertyInfos = GetProperties(interfaceMembers),
                MethodInfos = GetMethods(semanticModel, interfaceMembers, nullableContextEnabled),
                Events = GetEvents(interfaceMembers)
            };

            generatedSources.Add(new GeneratedInterfaceSource(
                key.NamespaceName,
                key.InterfaceName,
                interfaceCodeBuilderFactory().Build(symbolDetails)));
        }

        return new GeneratedInterfacesResult(generatedSources.ToImmutable(), warnings.ToImmutableArray());
    }

    private ImmutableArray<MethodInfo> GetMethods(SemanticModel semanticModel, List<ISymbol> members, bool nullableContextEnabled)
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
            methods.Add(GetMethodInfo(method, nullableContextEnabled));
        }

        return methods.ToImmutableArray();
    }

    private MethodInfo GetMethodInfo(IMethodSymbol method, bool nullableContextEnabled)
    {
        var parameters = method.Parameters.Select(methodParameter => GetParameterDisplayString(methodParameter, nullableContextEnabled)).ToList();

        var typedArgs = method.TypeParameters
            .Select(arg => (arg.ToDisplayString(FullyQualifiedDisplayFormat), roslynSymbols.GetWhereStatement(arg, FullyQualifiedDisplayFormat)))
            .ToList();

        return new MethodInfo(
            method.Name,
            GetMethodReturnType(method),
            InheritDoc(method),
            parameters.ToImmutableArray(),
            typedArgs.ToImmutableArray());
    }

    private static string GetMethodReturnType(IMethodSymbol method)
    {
        var prefix = method.ReturnsByRefReadonly ? "ref readonly " : method.ReturnsByRef ? "ref " : string.Empty;
        return prefix + method.ReturnType.ToDisplayString(FullyQualifiedDisplayFormat);
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

    private static ImmutableArray<EventInfo> GetEvents(List<ISymbol> members)
    {
        var events = new List<EventInfo>();
        foreach (var evt in members.Where(x => x.Kind == SymbolKind.Event)
                     .OfType<IEventSymbol>()
                     .GroupBy(x => x.ToDisplayString(FullyQualifiedDisplayFormatForGrouping))
                     .Select(g => g.First()))
        {
            events.Add(new EventInfo(evt.Name, evt.Type.ToDisplayString(FullyQualifiedDisplayFormat), InheritDoc(evt)));
        }

        return events.ToImmutableArray();
    }

    private static ImmutableArray<PropertyInfo> GetProperties(List<ISymbol> members)
    {
        var properties = new List<PropertyInfo>();
        foreach (var prop in members.Where(x => x.Kind == SymbolKind.Property)
                     .OfType<IPropertySymbol>()
                     .Where(x => !x.IsIndexer)
                     .GroupBy(x => x.Name)
                     .Select(g => g.First()))
        {
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

    private static bool HasIgnoreAttribute(ISymbol x)
    {
        const string ignoreInterfaceAttributeFullName = $"{Names.GlobalNamespacePrefix}{Names.IgnoreInterfaceAttributeFullName}";
        return x.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == ignoreInterfaceAttributeFullName);
    }

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

    private (string namespaceName, string interfaceName, bool asInternal) GetInterfaceGenerationSettings(
        Compilation compilation,
        AttributeData generationAttribute,
        string defaultNamespaceName,
        string defaultInterfaceName,
        bool defaultAsInternal)
    {
        if (TryGetSettingsFromSyntax(
                compilation,
                generationAttribute,
                defaultNamespaceName,
                defaultInterfaceName,
                defaultAsInternal) is { } settingsFromSyntax)
        {
            return settingsFromSyntax;
        }

        var args = arguments.GetArgs(
            generationAttribute.ConstructorArguments,
            generationAttribute.NamedArguments,
            Names.InterfaceNamespaceParameterName,
            Names.InterfaceNameParameterName,
            Names.InterfaceAsInternalParameterName);

        return (
            GetArgValue(args, 0, defaultNamespaceName),
            GetArgValue(args, 1, defaultInterfaceName),
            GetArgValue(args, 2, defaultAsInternal));
    }

    private (string namespaceName, string interfaceName, bool asInternal)? TryGetSettingsFromSyntax(
        Compilation compilation,
        AttributeData generationAttribute,
        string defaultNamespaceName,
        string defaultInterfaceName,
        bool defaultAsInternal)
    {
        if (generationAttribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax
            || attributeSyntax.ArgumentList == null)
        {
            return null;
        }

        var syntaxArgs = arguments.GetArgs(
            attributeSyntax.ArgumentList,
            Names.InterfaceNamespaceParameterName,
            Names.InterfaceNameParameterName,
            Names.InterfaceAsInternalParameterName);
        var semanticModel = compilation.GetSemanticModel(attributeSyntax.SyntaxTree);

        return (
            GetArgValue(semanticModel, syntaxArgs, 0, defaultNamespaceName),
            GetArgValue(semanticModel, syntaxArgs, 1, defaultInterfaceName),
            GetArgValue(semanticModel, syntaxArgs, 2, defaultAsInternal));
    }

    private static T GetArgValue<T>(IReadOnlyList<TypedConstant> args, int index, T defaultValue)
    {
        if (index >= args.Count)
        {
            return defaultValue;
        }

        var value = args[index].Value;
        return value is T typedValue ? typedValue : defaultValue;
    }

    private static T GetArgValue<T>(SemanticModel semanticModel, IReadOnlyList<AttributeArgumentSyntax?> args, int index, T defaultValue)
    {
        if (index >= args.Count)
        {
            return defaultValue;
        }

        var arg = args[index];
        if (arg == null)
        {
            return defaultValue;
        }

        var value = semanticModel.GetConstantValue(arg.Expression);
        return value is { HasValue: true, Value: T typedValue } ? typedValue : defaultValue;
    }

    private Location GetLocation(ISymbol member, AttributeData attribute)
    {
        if (attribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attributeSyntax)
        {
            return locationProvider.GetLocation(attributeSyntax);
        }

        return member.Locations.FirstOrDefault() ?? Location.None;
    }

    private static string GetInterfaceDisplayName(InterfaceKey key) =>
        string.IsNullOrWhiteSpace(key.NamespaceName)
            ? key.InterfaceName
            : $"{key.NamespaceName}.{key.InterfaceName}";

    private static string GetNamespaceName(INamespaceSymbol namespaceSymbol) =>
        namespaceSymbol.IsGlobalNamespace ? string.Empty : namespaceSymbol.ToDisplayString();

}
