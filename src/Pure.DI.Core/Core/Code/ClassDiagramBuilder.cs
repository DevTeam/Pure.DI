// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.

namespace Pure.DI.Core.Code;

using static LinesExtensions;

sealed class ClassDiagramBuilder(
    IFastBuilder<ContractsBuildContext, ISet<Injection>> injectionsBuilder,
    IMarker marker,
    ITypeResolver typeResolver,
    IRootAccessModifierResolver rootAccessModifierResolver,
    ITypes types,
    ILocationProvider locationProvider,
    IGlobalProperties globalProperties,
    ITypeSymbolComparer typeSymbolComparer,
    CancellationToken cancellationToken)
    : IBuilder<CompositionCode, Lines>
{
    private const string NullableReferenceTypeSuffix = "Ɂ";

    private static readonly FormatOptions DefaultFormatOptions = new();

    public Lines Build(CompositionCode composition)
    {
        var graph = composition.Source.Graph;
        if (graph.Edges.Count > globalProperties.MaxMermaid)
        {
            return new Lines();
        }

        var setup = composition.Setup;
        var nullable = composition.Compilation.Options.NullableContextOptions == NullableContextOptions.Disable ? "" : "?";
        var lines = new Lines();
        lines.AppendLine("---");
        lines.AppendLine(" config:");
        lines.AppendLine("  class:");
        lines.AppendLine("   hideEmptyMembersBox: true");
        lines.AppendLine("---");
        lines.AppendLine("classDiagram");
        using (lines.Indent())
        {
            var classes = new List<Class>();
            var hints = composition.Hints;
            var hasResolveMethods = hints.IsResolveEnabled;
            var rootProperties = composition.PublicRoots.ToDictionary(i => i.Injection, i => i);
            var compositionLines = new Lines();
            if (hasResolveMethods || rootProperties.Count > 0)
            {
                compositionLines.AppendLine($"class {composition.Setup.Name.ClassName} {BlockStart}");
                using (lines.Indent())
                {
                    compositionLines.AppendLine("<<partial>>");
                    foreach (var root in composition.PublicRoots.OrderByDescending(i => i.IsPublic).ThenBy(i => i.Name, StringComparer.Ordinal))
                    {
                        compositionLines.AppendLine($"{Format(rootAccessModifierResolver.Resolve(root))}{FormatRoot(setup, root)}");
                    }

                    if (hasResolveMethods)
                    {
                        var genericParameterT = $"{DefaultFormatOptions.StartGenericArgsSymbol}T{DefaultFormatOptions.FinishGenericArgsSymbol}";
                        compositionLines.AppendLine($"{(hints.ResolveMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}T {hints.ResolveMethodName}{genericParameterT}()");
                        compositionLines.AppendLine($"{(hints.ResolveByTagMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}T {hints.ResolveByTagMethodName}{genericParameterT}(object{nullable} tag)");
                        compositionLines.AppendLine($"{(hints.ObjectResolveMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}object {hints.ObjectResolveMethodName}(Type type)");
                        compositionLines.AppendLine($"{(hints.ObjectResolveByTagMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}object {hints.ObjectResolveByTagMethodName}(Type type, object{nullable} tag)");
                    }
                }

                compositionLines.AppendLine(BlockFinish);
            }
            else
            {
                compositionLines.AppendLine($"class {composition.Name.ClassName}");
            }

            var compositionClass = new Class(composition.Name.Namespace, composition.Name.ClassName, "", null, compositionLines);
            classes.Add(compositionClass);

            if (composition.TotalDisposablesCount > 0)
            {
                classes.Add(new Class(nameof(System), "IDisposable", "interface", null, new Lines()));
                lines.AppendLine($"{composition.Name.ClassName} --|> IDisposable");
            }

            if (composition.AsyncDisposableCount > 0)
            {
                classes.Add(new Class(nameof(System), "IAsyncDisposable", "interface", null, new Lines()));
                lines.AppendLine($"{composition.Name.ClassName} --|> IAsyncDisposable");
            }

            var typeSymbols = new HashSet<ITypeSymbol>(typeSymbolComparer.Dependency);
            foreach (var node in graph.Vertices.GroupBy(i => i.Type, typeSymbolComparer.Dependency).Select(i => i.First()).OrderBy(i => i.Binding.Id))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (node.Root is not null)
                {
                    continue;
                }

                var contracts = injectionsBuilder.Build(new ContractsBuildContext(node.Binding, MdTag.ContextTag, null));
                foreach (var contractGroup in contracts
                    .Where(c => !types.TypeEquals(node.Type, c.Type))
                    .GroupBy(c => c.Type, typeSymbolComparer.Dependency))
                {
                    var contractType = contractGroup.First().Type;
                    typeSymbols.Add(contractType);

                    var hasExplicitTag = contractGroup.Any(c => c.Tag is not null and not MdTagOnSites);
                    string label;
                    if (!hasExplicitTag)
                    {
                        label = "";
                    }
                    else
                    {
                        var parts = contractGroup
                            .Select(c => c.Tag is null or MdTagOnSites ? "default" : EscapeTag(c.Tag))
                            .Distinct()
                            .OrderBy(s => s == "default" ? 1 : 0)
                            .ThenBy(s => s, StringComparer.Ordinal)
                            .ToList();
                        label = $" : {string.Join(", ", parts)}";
                    }

                    lines.AppendLine($"{FormatType(setup, node.Type, DefaultFormatOptions)} --|> {FormatType(setup, contractType, DefaultFormatOptions)}{label}");
                }

                var classDiagramWalker = new ClassDiagramWalker(setup, marker, this, classes, DefaultFormatOptions, locationProvider);
                classDiagramWalker.VisitDependencyNode(new Lines(), node);
            }

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var type in typeSymbols.OrderBy(i => i.Name, StringComparer.Ordinal))
            {
                if (!marker.IsMarker(setup, type))
                {
                    classes.Add(new Class(ResolveNamespaceName(type.ContainingNamespace), FormatType(setup, type, DefaultFormatOptions), "", type, new Lines()));
                }
            }

            foreach (var (dependency, count) in graph.Edges.GroupBy(i => i).Select(i => (dependency: i.First(), count: i.Count())).OrderBy(i => i.dependency.Target.Binding.Id).ThenBy(i => i.dependency.Source.Binding.Id))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourceType = FormatType(setup, dependency.Source.Type, DefaultFormatOptions);
                if (!marker.IsMarker(setup, dependency.Source.Type))
                {
                    classes.Add(new Class(ResolveNamespaceName(dependency.Source.Type.ContainingNamespace), sourceType, "", dependency.Source.Type, new Lines()));
                }

                if (dependency.Target.Root is not null && rootProperties.TryGetValue(dependency.Injection, out var root))
                {
                    lines.AppendLine($"{composition.Name.ClassName} ..> {sourceType} : {FormatRoot(setup, root)}");
                }
                else
                {
                    var targetType = FormatType(setup, dependency.Target.Type, DefaultFormatOptions);
                    if (!marker.IsMarker(setup, dependency.Target.Type))
                    {
                        classes.Add(new Class(ResolveNamespaceName(dependency.Target.Type.ContainingNamespace), targetType, "", dependency.Target.Type, new Lines()));
                    }

                    if (dependency.Source.Arg is {} arg)
                    {
                        if (arg.Source.IsBuildUpInstance)
                        {
                            continue;
                        }

                        var tags = arg.Binding.Contracts.SelectMany(i => i.Tags.Select(tag => tag.Value)).ToList();
                        var formattedTags = tags.Count > 0 ? FormatTags(tags) : "";
                        var argLabel = formattedTags.Length > 0
                            ? $"{formattedTags} Argument \\\"{arg.Source.ArgName}\\\""
                            : $"Argument \\\"{arg.Source.ArgName}\\\"";
                        lines.AppendLine($"{targetType} o-- {sourceType} : {argLabel}");
                    }
                    else
                    {
                        if (types.TypeEquals(dependency.Source.Type, dependency.Target.Type))
                        {
                            continue;
                        }

                        var relationship = dependency.Source.Lifetime == Lifetime.Transient ? "*--" : "o--";
                        var cardinality = FormatCardinality(count, dependency.Source.Lifetime);
                        var head = cardinality.Length > 0
                            ? $"{targetType} {relationship} {cardinality} {sourceType}"
                            : $"{targetType} {relationship} {sourceType}";
                        lines.AppendLine($"{head} : {FormatDependency(setup, dependency, DefaultFormatOptions)}");
                    }
                }
            }

            foreach (var classByNamespace in classes.GroupBy(i => i.Namespace).OrderBy(i => i.Key, StringComparer.Ordinal))
            {
                var actualNamespace = classByNamespace.Key;
                var hasNamespace = !string.IsNullOrWhiteSpace(actualNamespace);
                if (hasNamespace)
                {
                    lines.AppendLine($"namespace {actualNamespace} {{");
                    lines.IncIndent();
                }

                foreach (var cls in classByNamespace.GroupBy(i => i.Name).Select(i => i.First()).OrderBy(i => i.Name, StringComparer.Ordinal))
                {
                    var classLines = cls.Lines;
                    if (classLines.Count > 0)
                    {
                        lines.AppendLines(cls.Lines);
                        continue;
                    }

                    lines.AppendLine($"class {cls.Name} {{");
                    var actualKind = cls.ActualKind;
                    if (!string.IsNullOrWhiteSpace(actualKind))
                    {
                        using (lines.Indent())
                        {
                            lines.AppendLine($"<<{actualKind}>>");
                        }
                    }

                    lines.AppendLine(BlockFinish);
                }

                // ReSharper disable once InvertIf
                if (hasNamespace)
                {
                    lines.DecIndent();
                    lines.AppendLine(BlockFinish);
                }
            }
        }

        return lines;
    }

    private string FormatRoot(MdSetup setup, Root root)
    {
        var typeArgsStr = "";
        var typeArgs = root.TypeDescription.TypeArgs;
        if (typeArgs.Count > 0)
        {
            typeArgsStr = $"{DefaultFormatOptions.StartGenericArgsSymbol}{string.Join(DefaultFormatOptions.TypeArgsSeparator, typeArgs.Select(arg => $"{arg}"))}{DefaultFormatOptions.FinishGenericArgsSymbol}";
        }

        var rootArgsStr = "";
        if (root.IsMethod)
        {
            rootArgsStr = $"({string.Join(", ", root.RootArgs.Select(arg => $"{ResolveTypeName(setup, arg.InstanceType)} {arg.Name}"))})";
        }

        return $"{FormatType(setup, root.Injection.Type, DefaultFormatOptions)} {root.DisplayName}{typeArgsStr}{rootArgsStr}";
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    private static string FormatCardinality(int count, Lifetime lifetime)
    {
        var parts = new List<string>(2);
        if (count > 1)
        {
            parts.Add(count.ToString());
        }

        if (lifetime != Lifetime.Transient)
        {
            parts.Add(lifetime.ToString());
        }

        if (count > 1)
        {
            parts.Add("instances");
        }

        return parts.Count == 0 ? "" : $"\\\"{string.Join(" ", parts)}\\\"";
    }

    private string FormatDependency(MdSetup setup, Dependency dependency, FormatOptions options)
    {
        var tag = FormatTag(dependency.Injection.Tag);
        var symbol = FormatSymbol(setup, dependency.Injection.Type, options);
        return tag.Length == 0 ? symbol : $"{tag} {symbol}";
    }

    private static string FormatTag(object? tag) =>
        tag is null or MdTagOnSites
            ? ""
            : EscapeTag(tag);

    private static string EscapeTag(object tag) =>
        tag.ValueToString("")
            .Replace("\"", "\\\"")
            .Replace(':', '﹕');

    private static string FormatTags(IEnumerable<object?> tags) =>
        string.Join(", ", tags.Distinct().Select(FormatTag).Where(s => s.Length > 0).OrderBy(i => i, StringComparer.Ordinal));

    private string FormatSymbol(MdSetup setup, ISymbol? symbol, FormatOptions options) =>
        symbol switch
        {
            IParameterSymbol parameter => FormatParameter(setup, parameter, options),
            IPropertySymbol property => FormatProperty(setup, property, options),
            IFieldSymbol field => FormatField(setup, field, options),
            IMethodSymbol method => FormatMethod(setup, method, options),
            ITypeSymbol type => FormatType(setup, type, options),
            _ => symbol?.ToString() ?? ""
        };

    private string FormatMethod(MdSetup setup, IMethodSymbol method, FormatOptions options)
    {
        if (method is { Kind: SymbolKind.Property, ContainingSymbol: IPropertySymbol property })
        {
            return $"{Format(method.DeclaredAccessibility)}{property.Name} : {FormatType(setup, method.ReturnType, options)}";
        }

        // ReSharper disable once InvertIf
        if (method.MethodKind == MethodKind.Constructor)
        {
            return $"{Format(method.DeclaredAccessibility)}{method.ContainingType.Name}({string.Join(", ", method.Parameters.Select(FormatPropertyLocal))})";
            string FormatPropertyLocal(IParameterSymbol parameter) => $"{FormatType(setup, parameter.Type, options)} {parameter.Name}";
        }

        return $"{Format(method.DeclaredAccessibility)}{method.Name}({string.Join(", ", method.Parameters.Select(FormatParameterLocal))}) : {FormatType(setup, method.ReturnType, options)}";

        string FormatParameterLocal(IParameterSymbol parameter) => $"{FormatType(setup, parameter.Type, options)} {parameter.Name}";
    }

    private string FormatProperty(MdSetup setup, IPropertySymbol property, FormatOptions options) =>
        $"{Format(property.GetMethod?.DeclaredAccessibility ?? property.DeclaredAccessibility)}{FormatType(setup, property.Type, options)} {property.Name}";

    private string FormatField(MdSetup setup, IFieldSymbol field, FormatOptions options) =>
        $"{Format(field.DeclaredAccessibility)}{FormatType(setup, field.Type, options)} {field.Name}";

    private string FormatField(MdSetup setup, IField field, FormatOptions options) =>
        $"{Format(field.DeclaredAccessibility)}{FormatType(setup, field.Type, options)} {field.Name}";

    private string FormatParameter(MdSetup setup, IParameterSymbol parameter, FormatOptions options) =>
        $"{FormatType(setup, parameter.Type, options)} {parameter.Name}";

    private string FormatType(MdSetup setup, ISymbol? symbol, FormatOptions options)
    {
        if (symbol is ITypeSymbol typeSymbol && marker.IsMarker(setup, typeSymbol))
        {
            return ResolveTypeName(setup, typeSymbol);
        }

        return symbol switch
        {
            INamedTypeSymbol { IsGenericType: true } namedTypeSymbol => $"{namedTypeSymbol.Name}{options.StartGenericArgsSymbol}{string.Join(options.TypeArgsSeparator, namedTypeSymbol.TypeArguments.Select(FormatSymbolLocal))}{options.FinishGenericArgsSymbol}{FormatNullableSuffix(namedTypeSymbol)}",
            IArrayTypeSymbol array => $"Array{options.StartGenericArgsSymbol}{FormatType(setup, array.ElementType, options)}{options.FinishGenericArgsSymbol}{FormatNullableSuffix(array)}",
            ITypeSymbol type => $"{type.Name}{FormatNullableSuffix(type)}",
            _ => symbol?.Name ?? "Unresolved"
        };

        string FormatSymbolLocal(ITypeSymbol i) => FormatSymbol(setup, i, options);
    }

    private static string FormatNullableSuffix(ITypeSymbol type) =>
        type is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated } ? NullableReferenceTypeSuffix : "";

    private string ResolveTypeName(MdSetup setup, ITypeSymbol typeSymbol)
    {
        var typeName = typeResolver.Resolve(setup, typeSymbol).Name.Replace("?", NullableReferenceTypeSuffix);
        return typeName.StartsWith(Names.GlobalNamespacePrefix) ? typeName[Names.GlobalNamespacePrefix.Length..] : typeName;
    }

    private static string ResolveNamespaceName(INamespaceSymbol? namespaceSymbol) =>
        namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace ? "" : namespaceSymbol.ToDisplayString();

    private static string Format(Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Private => "-",
            Accessibility.ProtectedAndInternal => "#",
            Accessibility.Protected => "#",
            Accessibility.Internal => "~",
            Accessibility.ProtectedOrInternal => "~",
            Accessibility.Public => "+",
            _ => ""
        };

    private record FormatOptions(
        string StartGenericArgsSymbol = "ᐸ",
        string FinishGenericArgsSymbol = "ᐳ",
        string TypeArgsSeparator = "ˏ");

    private class ClassDiagramWalker(
        MdSetup setup,
        IMarker marker,
        ClassDiagramBuilder builder,
        IList<Class> classes,
        FormatOptions options,
        ILocationProvider locationProvider)
        : DependenciesWalker<Lines>(locationProvider)
    {
        public override void VisitDependencyNode(in Lines ctx, DependencyNode node)
        {
            var lines = new Lines();
            var name = builder.FormatType(setup, node.Type, options);
            var cls = new Class(ResolveNamespaceName(node.Type.ContainingNamespace), name, "", node.Type, lines);
            if (!marker.IsMarker(setup, node.Type))
            {
                classes.Add(cls);
            }

            lines.AppendLine($"class {name} {{");
            using (lines.Indent())
            {
                var actualKind = cls.ActualKind;
                if (!string.IsNullOrWhiteSpace(actualKind))
                {
                    using (lines.Indent())
                    {
                        lines.AppendLine($"<<{actualKind}>>");
                    }
                }

                base.VisitDependencyNode(lines, node);
            }

            lines.AppendLine(BlockFinish);
        }

        public override void VisitConstructor(in Lines ctx, in DpMethod constructor)
        {
            ctx.AppendLine(builder.FormatMethod(setup, constructor.Method, options));
            base.VisitConstructor(ctx, in constructor);
        }

        public override void VisitMethod(in Lines ctx, in DpMethod method, int? position)
        {
            ctx.AppendLine(builder.FormatMethod(setup, method.Method, options));
            base.VisitMethod(ctx, in method, position);
        }

        public override void VisitProperty(in Lines ctx, in DpProperty property, int? position)
        {
            ctx.AppendLine(builder.FormatProperty(setup, property.Property, options));
            base.VisitProperty(ctx, in property, position);
        }

        public override void VisitField(in Lines ctx, in DpField field, int? position)
        {
            ctx.AppendLine(builder.FormatField(setup, field.Field, options));
            base.VisitField(ctx, in field, position);
        }
    }

    private record Class(string Namespace, string Name, string Kind, ITypeSymbol? Type, Lines Lines)
    {
        public string ActualKind
        {
            get
            {
                var typeKind = Kind;
                if (Type == null)
                {
                    return typeKind;
                }

                if (Type.IsUnmanagedType)
                {
                    typeKind = "unmanaged";
                }

                if (Type.IsAnonymousType)
                {
                    typeKind = "anonymous";
                }

                typeKind = Type.TypeKind switch
                {
                    TypeKind.Interface or TypeKind.Enum or TypeKind.Delegate or TypeKind.Struct => Type.TypeKind.ToString().ToLowerInvariant(),
                    _ => typeKind
                };

                if (Type is IArrayTypeSymbol)
                {
                    typeKind = "array";
                }

                if (Type.IsAbstract && Type.TypeKind == TypeKind.Class)
                {
                    typeKind = "abstract";
                }

                if (Type.IsRecord)
                {
                    typeKind = "record";
                }

                if (Type.IsTupleType)
                {
                    typeKind = "tuple";
                }

                return string.IsNullOrWhiteSpace(typeKind) ? Type.TypeKind.ToString().ToLowerInvariant() : typeKind;
            }
        }
    }
}
