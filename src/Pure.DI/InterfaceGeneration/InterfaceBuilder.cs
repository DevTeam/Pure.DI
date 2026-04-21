namespace Pure.DI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class InterfaceBuilder
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

    public static string BuildInterfaceFor(ITypeSymbol typeSymbol, ClassDeclarationSyntax classSyntax)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return string.Empty;
        }

        var generationAttribute = typeSymbol.GetAttributes().FirstOrDefault(x =>
            x.AttributeClass != null && x.AttributeClass.Name.Contains(InterfaceGenerator.GenerateInterfaceAttributeName));

        var symbolDetails = new GeneratedInterfaceDetails(generationAttribute, typeSymbol, classSyntax);
        var interfaceGenerator = new InterfaceCodeBuilder(symbolDetails.NamespaceName, symbolDetails.InterfaceName, symbolDetails.AccessLevel);

        interfaceGenerator.AddClassDocumentation(GetDocumentationForClass(classSyntax));
        interfaceGenerator.AddGeneric(GetGeneric(classSyntax, namedTypeSymbol));

        var members = typeSymbol.GetAllMembers()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => !x.IsStatic)
            .Where(x => !HasIgnoreAttribute(x))
            .ToList();

        AddPropertiesToInterface(members, interfaceGenerator);
        AddMethodsToInterface(members, interfaceGenerator);
        AddEventsToInterface(members, interfaceGenerator);

        return interfaceGenerator.Build();
    }

    private static void AddMethodsToInterface(List<ISymbol> members, InterfaceCodeBuilder codeGenerator)
    {
        members.Where(x => x.Kind == SymbolKind.Method)
            .OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Ordinary)
            .Where(x => x.ContainingType.Name != nameof(System.Object))
            .Where(x => !HasIgnoreAttribute(x))
            .GroupBy(x => x.ToDisplayString(FullyQualifiedDisplayFormatForGrouping))
            .Select(g => g.First())
            .ToList()
            .ForEach(method => AddMethod(codeGenerator, method));
    }

    private static void AddMethod(InterfaceCodeBuilder codeGenerator, IMethodSymbol method)
    {
        ActivateNullableIfNeeded(codeGenerator, method);

        var paramResult = new HashSet<string>();
        foreach (var parameter in method.Parameters.Select(p => GetParameterDisplayString(p, codeGenerator.HasNullable)))
        {
            paramResult.Add(parameter);
        }

        var typedArgs = method.TypeParameters.Select(arg => (arg.ToDisplayString(FullyQualifiedDisplayFormat), arg.GetWhereStatement(FullyQualifiedDisplayFormat))).ToList();

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

        if (param.HasExplicitDefaultValue
            && param.ExplicitDefaultValue is null
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
            _ => setMethodSymbol is { DeclaredAccessibility: Accessibility.Public } ? PropertySetKind.Always : PropertySetKind.NoSet,
        };

    private static bool HasIgnoreAttribute(ISymbol x) =>
        x.GetAttributes().Any(a => a.AttributeClass != null && a.AttributeClass.Name.Contains(InterfaceGenerator.IgnoreInterfaceAttributeName));

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
            SyntaxKind.SingleLineDocumentationCommentTrivia,
        ];

        var trivia = classSyntax.GetLeadingTrivia()
            .Where(x => docSyntax.Contains(x.Kind()))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToFullString()));

        return trivia.ToFullString().Trim();
    }

    private static string GetGeneric(TypeDeclarationSyntax classSyntax, INamedTypeSymbol typeSymbol)
    {
        var whereStatements = typeSymbol.TypeParameters.Select(typeParameter => typeParameter.GetWhereStatement(FullyQualifiedDisplayFormat)).Where(constraint => !string.IsNullOrEmpty(constraint));
        return $"{classSyntax.TypeParameterList?.ToFullString().Trim()} {string.Join(" ", whereStatements)}".Trim();
    }

    private static string InheritDoc(ISymbol source) =>
        $"/// <inheritdoc cref=\"{source.ToDisplayString().Replace("<", "{").Replace(">", "}").Replace("params ", string.Empty)}\" />";
}

internal sealed class InterfaceCodeBuilder(string namespaceName, string interfaceName, string accessLevel)
{
    private const string Autogenerated = """
        //--------------------------------------------------------------------------------------------------
        // <auto-generated>
        //     This code was generated by a tool.
        //
        //     Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
        // </auto-generated>
        //--------------------------------------------------------------------------------------------------

        
        """;

    private readonly List<PropertyInfo> propertyInfos = [];
    private readonly List<MethodInfo> methodInfos = [];
    private readonly List<EventInfo> events = [];
    private string classDocumentation = string.Empty;
    private string genericType = string.Empty;

    public bool HasNullable { get; set; }

    public void AddPropertyToInterface(string name, string ttype, bool hasGet, PropertySetKind hasSet, bool isRef, string documentation) =>
        propertyInfos.Add(new(name, ttype, hasGet, hasSet, isRef, documentation));

    public void AddGeneric(string v) => genericType = v;

    public void AddClassDocumentation(string documentation) => classDocumentation = documentation;

    public void AddMethodToInterface(string name, string returnType, string documentation, HashSet<string> parameters, List<(string Arg, string WhereConstraint)> genericArgs) =>
        methodInfos.Add(new(name, returnType, documentation, parameters, genericArgs));

    public void AddEventToInterface(string name, string type, string documentation) =>
        events.Add(new(name, type, documentation));

    public string Build()
    {
        var cb = new CodeBuilder();
        cb.Append(Autogenerated);

        if (HasNullable)
        {
            cb.AppendLine("#nullable enable");
        }

        cb.AppendLine("using System;");
        cb.AppendLine(string.Empty);

        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            cb.AppendLine($"namespace {namespaceName}");
            cb.AppendLine("{");
            cb.Indent();
        }

        cb.AppendAndNormalizeMultipleLines(classDocumentation);
        cb.AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"Pure.DI\", \"\")]");
        cb.AppendLine($"{accessLevel} partial interface {interfaceName}{genericType}");
        cb.AppendLine("{");
        cb.Indent();

        foreach (var prop in propertyInfos)
        {
            cb.AppendAndNormalizeMultipleLines(prop.Documentation);
            var @ref = prop.IsRef ? "ref " : string.Empty;
            var get = prop.HasGet ? "get; " : string.Empty;
            var set = GetSet(prop.SetKind);
            cb.AppendLine($"{@ref}{prop.Ttype} {prop.Name} {{ {get}{set}}}");
            cb.AppendLine(string.Empty);
        }

        foreach (var method in methodInfos)
        {
            BuildMethod(cb, method);
        }

        foreach (var evt in events)
        {
            cb.AppendAndNormalizeMultipleLines(evt.Documentation);
            cb.AppendLine($"event {evt.Type} {evt.Name};");
            cb.AppendLine(string.Empty);
        }

        cb.Dedent();
        cb.AppendLine("}");

        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            cb.Dedent();
            cb.AppendLine("}");
        }

        if (HasNullable)
        {
            cb.AppendLine("#nullable restore");
        }

        return cb.Build();
    }

    private static string GetSet(PropertySetKind propSetKind) =>
        propSetKind switch
        {
            PropertySetKind.NoSet => string.Empty,
            PropertySetKind.Always => "set; ",
            PropertySetKind.Init => "init; ",
            _ => throw new ArgumentOutOfRangeException(nameof(propSetKind), propSetKind, null),
        };

    private static void BuildMethod(CodeBuilder cb, MethodInfo method)
    {
        cb.AppendAndNormalizeMultipleLines(method.Documentation);
        cb.AppendIndented($"{method.ReturnType} {method.Name}");

        if (method.GenericArgs.Count != 0)
        {
            cb.Append($"<{string.Join(", ", method.GenericArgs.Select(a => a.Arg))}>");
        }

        cb.Append($"({string.Join(", ", method.Parameters)})");

        if (method.GenericArgs.Count != 0)
        {
            var constraints = method.GenericArgs.Where(a => !string.IsNullOrWhiteSpace(a.WhereConstraint)).Select(a => a.WhereConstraint);
            cb.Append($" {string.Join(" ", constraints)}");
        }

        cb.Append(";");
        cb.BreakLine();
        cb.AppendLine(string.Empty);
    }
}

internal sealed record PropertyInfo(string Name, string Ttype, bool HasGet, PropertySetKind SetKind, bool IsRef, string Documentation);

internal sealed record MethodInfo(string Name, string ReturnType, string Documentation, HashSet<string> Parameters, List<(string Arg, string WhereConstraint)> GenericArgs);

internal sealed record EventInfo(string Name, string Type, string Documentation);

internal enum PropertySetKind
{
    NoSet = 0,
    Always = 1,
    Init = 2,
}

internal sealed class CodeBuilder
{
    private readonly StringBuilder sb = new();
    private int indent;
    private string currentIndent = string.Empty;

    public void Indent()
    {
        indent += 4;
        currentIndent = new(' ', indent);
    }

    public void Dedent()
    {
        indent -= 4;
        currentIndent = new(' ', indent);
    }

    public void BreakLine() => sb.AppendLine();

    public void AppendIndented(string str)
    {
        sb.Append(' ', indent);
        sb.Append(str);
    }

    public void AppendLine(string str)
    {
        sb.Append(' ', indent);
        sb.AppendLine(str);
    }

    public void Append(string str) => sb.Append(str);

    public void AppendAndNormalizeMultipleLines(string doc)
    {
        if (string.IsNullOrWhiteSpace(doc))
        {
            return;
        }

        foreach (var line in doc.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
        {
            sb.AppendLine(IndentStr(line));
        }
    }

    private string IndentStr(string str) => str.TrimStart().Insert(0, currentIndent);

    public string Build() => sb.ToString();
}
