// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

internal sealed class ClassDiagramBuilder(
    IBuilder<ContractsBuildContext, ISet<Injection>> injectionsBuilder,
    IMarker marker,
    ITypeResolver typeResolver,
    CancellationToken cancellationToken)
    : IBuilder<CompositionCode, LinesBuilder>
{
    private static readonly FormatOptions DefaultFormatOptions = new();

    public LinesBuilder Build(CompositionCode composition)
    {
        var setup = composition.Source.Source;
        var nullable = composition.Compilation.Options.NullableContextOptions == NullableContextOptions.Disable ? "" : "?";
        var lines = new LinesBuilder();
        lines.AppendLine("classDiagram");
        using (lines.Indent())
        {
            var hasResolveMethods = composition.Source.Source.Hints.IsResolveEnabled;
            var rootProperties = composition.Roots.ToDictionary(i => i.Injection, i => i);
            if (hasResolveMethods || rootProperties.Any())
            {
                lines.AppendLine($"class {composition.Source.Source.Name.ClassName} {{");
                using (lines.Indent())
                {
                    lines.AppendLine("<<partial>>");
                    foreach (var root in composition.Roots.OrderByDescending(i => i.IsPublic).ThenBy(i => i.Name))
                    {
                        lines.AppendLine($"{(root.IsPublic ? "+" : "-")}{FormatRoot(setup, root)}");
                    }

                    if (hasResolveMethods)
                    {
                        var hints = composition.Source.Source.Hints;
                        var genericParameterT = $"{DefaultFormatOptions.StartGenericArgsSymbol}T{DefaultFormatOptions.FinishGenericArgsSymbol}";
                        lines.AppendLine($"{(hints.ResolveMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}T {hints.ResolveMethodName}{genericParameterT}()");
                        lines.AppendLine($"{(hints.ResolveByTagMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}T {hints.ResolveByTagMethodName}{genericParameterT}(object{nullable} tag)");
                        lines.AppendLine($"{(hints.ObjectResolveMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}object {hints.ObjectResolveMethodName}(Type type)");
                        lines.AppendLine($"{(hints.ObjectResolveByTagMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}object {hints.ObjectResolveByTagMethodName}(Type type, object{nullable} tag)");
                    }
                }

                lines.AppendLine("}");
            }
            else
            {
                lines.AppendLine($"class {composition.Source.Source.Name.ClassName}");
            }

            if (composition.TotalDisposablesCount > 0)
            {
                lines.AppendLine($"{composition.Source.Source.Name.ClassName} --|> IDisposable");
            }

            if (composition.AsyncDisposableCount > 0)
            {
                lines.AppendLine($"{composition.Source.Source.Name.ClassName} --|> IAsyncDisposable");
            }

            var types = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var graph = composition.Source.Graph;
            foreach (var node in graph.Vertices.GroupBy(i => i.Type, SymbolEqualityComparer.Default).Select(i => i.First()))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (node.Root is not null)
                {
                    continue;
                }

                var contracts = injectionsBuilder.Build(new ContractsBuildContext(node.Binding, MdTag.ContextTag));
                foreach (var contract in contracts)
                {
                    if (node.Type.Equals(contract.Type, SymbolEqualityComparer.Default))
                    {
                        continue;
                    }

                    types.Add(contract.Type);
                    var tag = FormatTag(contract.Tag);
                    lines.AppendLine($"{FormatType(setup, node.Type, DefaultFormatOptions)} --|> {FormatType(setup, contract.Type, DefaultFormatOptions)}{(string.IsNullOrWhiteSpace(tag) ? "" : $" : {tag}")}");
                }

                var classDiagramWalker = new ClassDiagramWalker(setup, this, lines, DefaultFormatOptions);
                classDiagramWalker.VisitDependencyNode(Unit.Shared, node);
            }

            foreach (var type in types)
            {
                var typeKind = "";
                if (type.IsRecord)
                {
                    typeKind = "record";
                }

                if (type.IsTupleType)
                {
                    typeKind = "tuple";
                }

                if (type.IsAbstract)
                {
                    typeKind = "abstract";
                }

                typeKind = type.TypeKind switch
                {
                    TypeKind.Interface or TypeKind.Enum or TypeKind.Delegate or TypeKind.Struct => type.TypeKind.ToString().ToLower(),
                    _ => typeKind
                };

                if (string.IsNullOrWhiteSpace(typeKind))
                {
                    continue;
                }

                lines.AppendLine($"class {FormatType(setup, type, DefaultFormatOptions)} {{");
                using (lines.Indent())
                {
                    lines.AppendLine($"<<{typeKind}>>");
                }

                lines.AppendLine("}");
            }

            foreach (var (dependency, count) in graph.Edges.GroupBy(i => i).Select(i => (dependency: i.First(), count: i.Count())))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (dependency.Target.Root is not null && rootProperties.TryGetValue(dependency.Injection, out var root))
                {
                    lines.AppendLine($"{composition.Source.Source.Name.ClassName} ..> {FormatType(setup, dependency.Source.Type, DefaultFormatOptions)} : {FormatRoot(setup, root)}");
                }
                else
                {
                    if (dependency.Source.Arg is { } arg)
                    {
                        var tags = arg.Binding.Contracts.SelectMany(i => i.Tags.Select(tag => tag.Value)).ToArray();
                        lines.AppendLine($"{FormatType(setup, dependency.Target.Type, DefaultFormatOptions)} o-- {FormatType(setup, dependency.Source.Type, DefaultFormatOptions)} : {(tags.Any() ? FormatTags(tags) + " " : "")}Argument \\\"{arg.Source.ArgName}\\\"");
                    }
                    else
                    {
                        if (SymbolEqualityComparer.Default.Equals(dependency.Source.Type, dependency.Target.Type))
                        {
                            continue;
                        }

                        var relationship = dependency.Source.Lifetime == Lifetime.Transient ? "*--" : "o--";
                        lines.AppendLine($"{FormatType(setup, dependency.Target.Type, DefaultFormatOptions)} {relationship} {FormatCardinality(count, dependency.Source.Lifetime)} {FormatType(setup, dependency.Source.Type, DefaultFormatOptions)} : {FormatDependency(setup, dependency, DefaultFormatOptions)}");
                    }
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
            rootArgsStr = $"({string.Join(", ", root.Args.Select(arg => $"{ResolveTypeName(setup, arg.InstanceType)} {arg.VariableDeclarationName}"))})";
        }

        var displayName = root.IsPublic ? root.DisplayName : "_";
        return $"{FormatType(setup, root.Injection.Type, DefaultFormatOptions)} {displayName}{typeArgsStr}{rootArgsStr}";
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    private static string FormatCardinality(int count, Lifetime lifetime)
    {
        var cardinality = new StringBuilder();
        if (count > 1)
        {
            cardinality.Append(count);
            cardinality.Append(' ');
        }

        if (lifetime != Lifetime.Transient)
        {
            cardinality.Append(lifetime);
        }

        if (cardinality.Length > 0)
        {
            cardinality.Insert(0, "\\\"");
            cardinality.Append("\\\"");
        }

        return cardinality.ToString();
    }

    private string FormatDependency(MdSetup setup, Dependency dependency, FormatOptions options) =>
        $"{(dependency.Injection.Tag is null or MdTagOnSites ? "" : FormatTag(dependency.Injection.Tag) + " ")}{FormatSymbol(setup, dependency.Injection.Type, options)}";

    private static string FormatTag(object? tag) =>
        tag is null or MdTagOnSites
            ? ""
            : EscapeTag(tag) + " ";

    private static string EscapeTag(object tag) =>
        tag.ValueToString("")
            .Replace("\"", "\\\"")
            .Replace(':', '﹕');

    private static string FormatTags(IEnumerable<object?> tags) =>
        string.Join(", ", tags.Distinct().Select(FormatTag).OrderBy(i => i));

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
            INamedTypeSymbol { IsGenericType: true } namedTypeSymbol => $"{namedTypeSymbol.Name}{options.StartGenericArgsSymbol}{string.Join(options.TypeArgsSeparator, namedTypeSymbol.TypeArguments.Select(FormatSymbolLocal))}{options.FinishGenericArgsSymbol}",
            IArrayTypeSymbol array => $"Array{options.StartGenericArgsSymbol}{FormatType(setup, array.ElementType, options)}{options.FinishGenericArgsSymbol}",
            _ => symbol?.Name ?? "Unresolved"
        };

        string FormatSymbolLocal(ITypeSymbol i) => FormatSymbol(setup, i, options);
    }

    private string ResolveTypeName(MdSetup setup, ITypeSymbol typeSymbol)
    {
        var typeName = typeResolver.Resolve(setup, typeSymbol).Name;
        return typeName.StartsWith(Names.GlobalNamespacePrefix) ? typeName[Names.GlobalNamespacePrefix.Length..] : typeName;
    }

    private static string Format(Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.NotApplicable => "",
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
        ClassDiagramBuilder builder,
        LinesBuilder lines,
        FormatOptions options)
        : DependenciesWalker<Unit>
    {
        private readonly LinesBuilder _nodeLines = new();

        public override void VisitDependencyNode(in Unit ctx, DependencyNode node)
        {
            base.VisitDependencyNode(ctx, node);
            if (!_nodeLines.Lines.Any())
            {
                lines.AppendLine($"class {builder.FormatType(setup, node.Type, options)}");
                return;
            }

            lines.AppendLine($"class {builder.FormatType(setup, node.Type, options)} {{");
            using (lines.Indent())
            {
                lines.AppendLines(_nodeLines.Lines);
            }

            lines.AppendLine("}");
        }

        public override void VisitConstructor(in Unit ctx, in DpMethod constructor)
        {
            _nodeLines.AppendLine(builder.FormatMethod(setup, constructor.Method, options));
            base.VisitConstructor(ctx, in constructor);
        }

        public override void VisitMethod(in Unit ctx, in DpMethod method)
        {
            _nodeLines.AppendLine(builder.FormatMethod(setup, method.Method, options));
            base.VisitMethod(ctx, in method);
        }

        public override void VisitProperty(in Unit ctx, in DpProperty property)
        {
            _nodeLines.AppendLine(builder.FormatProperty(setup, property.Property, options));
            base.VisitProperty(ctx, in property);
        }

        public override void VisitField(in Unit ctx, in DpField field)
        {
            _nodeLines.AppendLine(builder.FormatField(setup, field.Field, options));
            base.VisitField(ctx, in field);
        }
    }
}