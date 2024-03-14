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
                    foreach (var root in composition.Roots.OrderByDescending(i => i.IsPublic).ThenBy(i => i.Name))
                    {
                        lines.AppendLine($"{(root.IsPublic ? "+" : "-")}{FormatRoot(root)}");
                    }
                    
                    if (hasResolveMethods)
                    {
                        var hints = composition.Source.Source.Hints;
                        var genericParameterT = $"{DefaultFormatOptions.StartGenericArgsSymbol}T{DefaultFormatOptions.FinishGenericArgsSymbol}";
                        lines.AppendLine($"{(hints.ResolveMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}T {hints.ResolveMethodName}{genericParameterT}()");
                        lines.AppendLine($"{(hints.ResolveByTagMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}T {hints.ResolveByTagMethodName}{genericParameterT}(object? tag)");
                        lines.AppendLine($"{(hints.ObjectResolveMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}object {hints.ObjectResolveMethodName}(Type type)");
                        lines.AppendLine($"{(hints.ObjectResolveByTagMethodModifiers == Names.DefaultApiMethodModifiers ? "+ " : "")}object {hints.ObjectResolveByTagMethodName}(Type type, object? tag)");
                    }
                }

                lines.AppendLine("}");
            }
            else
            {
                lines.AppendLine($"class {composition.Source.Source.Name.ClassName}");
            }

            if (composition.DisposablesCount > 0)
            {
                lines.AppendLine($"{composition.Source.Source.Name.ClassName} --|> IDisposable");
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
                    lines.AppendLine($"{FormatType(node.Type, DefaultFormatOptions)} --|> {FormatType(contract.Type, DefaultFormatOptions)} : {FormatTag(contract.Tag)}");
                }

                var classDiagramWalker = new ClassDiagramWalker(this, lines, DefaultFormatOptions);
                classDiagramWalker.VisitDependencyNode(Unit.Shared, node);
            }

            foreach (var type in types)
            {
                var typeKind = "";
                if (type.IsRecord)
                {
                    typeKind = "record";
                }
                else
                {
                    if (type.IsTupleType)
                    {
                        typeKind = "tuple";
                    }
                    else
                    {
                        if (type.IsAbstract)
                        {
                            typeKind = "abstract";
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(typeKind))
                {
                    continue;
                }

                lines.AppendLine($"class {FormatType(type, DefaultFormatOptions)} {{");
                using (lines.Indent())
                {
                    lines.AppendLine($"<<{typeKind}>>");    
                }
                lines.AppendLine("}");
            }
            
            foreach (var dependency in graph.Edges)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (dependency.Target.Root is not null && rootProperties.TryGetValue(dependency.Injection, out var root))
                {
                    lines.AppendLine($"{composition.Source.Source.Name.ClassName} ..> {FormatType(dependency.Source.Type, DefaultFormatOptions)} : {FormatRoot(root)}");
                }
                else
                {
                    if (dependency.Source.Arg is { } arg)
                    {
                        var tags = arg.Binding.Contracts.SelectMany(i => i.Tags.Select(tag => tag.Value)).ToArray();
                        lines.AppendLine($"{FormatType(dependency.Target.Type, DefaultFormatOptions)} o-- {FormatType(dependency.Source.Type, DefaultFormatOptions)} : {(tags.Any() ? FormatTags(tags) + " " : "")}Argument \\\"{arg.Source.ArgName}\\\"");
                    }
                    else
                    {
                        if (SymbolEqualityComparer.Default.Equals(dependency.Source.Type, dependency.Target.Type))
                        {
                            continue;
                        }

                        var relationship = dependency.Source.Lifetime == Lifetime.Transient ? "*--" : "o--";
                        lines.AppendLine($"{FormatType(dependency.Target.Type, DefaultFormatOptions)} {relationship} {FormatCardinality(dependency.Source.Lifetime)} {FormatType(dependency.Source.Type, DefaultFormatOptions)} : {FormatDependency(dependency, DefaultFormatOptions)}");   
                    }
                }
            }
        }
        
        return lines;
    }

    private string FormatRoot(Root root)
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
            rootArgsStr = $"({string.Join(", ", root.Args.Select(arg => $"{typeResolver.Resolve(arg.InstanceType)} {arg.VariableName}"))})";
        }

        return $"{FormatType(root.Injection.Type, DefaultFormatOptions)} {root.DisplayName}{typeArgsStr}{rootArgsStr}";
    }

    private static string FormatCardinality(Lifetime lifetime) =>
        lifetime switch
        {
            Lifetime.Transient => "",
            _ => $" \\\"{lifetime}\\\""
        };
    
    private string FormatDependency(Dependency dependency, FormatOptions options) => 
        $"{(dependency.Injection.Tag == default ? "" : FormatTag(dependency.Injection.Tag) + " ")}{FormatSymbol(dependency.Injection.Type, options)}";

    private static string FormatTag(object? tag) =>
        tag != default ? $"{tag.ValueToString("").Replace("\"", "\\\"")} " : "";
    
    private static string FormatTags(IEnumerable<object?> tags) =>
        string.Join(", ", tags.Distinct().Select(FormatTag).OrderBy(i => i));

    private string FormatSymbol(ISymbol? symbol, FormatOptions options) =>
        symbol switch
        {
            IParameterSymbol parameter => FormatParameter(parameter, options),
            IPropertySymbol property => FormatProperty(property, options),
            IFieldSymbol field => FormatField(field, options),
            IMethodSymbol method => FormatMethod(method, options),
            ITypeSymbol type => FormatType(type, options),
            _ => symbol?.ToString() ?? ""
        };

    private string FormatMethod(IMethodSymbol method, FormatOptions options)
    {
        if (method is { Kind: SymbolKind.Property, ContainingSymbol: IPropertySymbol property })
        {
            return $"{Format(method.DeclaredAccessibility)}{property.Name} : {FormatType(method.ReturnType, options)}";
        }

        // ReSharper disable once InvertIf
        if (method.MethodKind == MethodKind.Constructor)
        {
            return $"{Format(method.DeclaredAccessibility)}{method.ContainingType.Name}({string.Join(", ", method.Parameters.Select(FormatPropertyLocal))})";
            string FormatPropertyLocal(IParameterSymbol parameter) => $"{FormatType(parameter.Type, options)} {parameter.Name}";
        }

        return $"{Format(method.DeclaredAccessibility)}{method.Name}({string.Join(", ", method.Parameters.Select(FormatParameterLocal))}) : {FormatType(method.ReturnType, options)}";

        string FormatParameterLocal(IParameterSymbol parameter) => $"{FormatType(parameter.Type, options)} {parameter.Name}";
    }

    private string FormatProperty(IPropertySymbol property, FormatOptions options) =>
        $"{Format(property.GetMethod?.DeclaredAccessibility ?? property.DeclaredAccessibility)}{FormatType(property.Type, options)} {property.Name}";

    private string FormatField(IFieldSymbol field, FormatOptions options) =>
        $"{Format(field.DeclaredAccessibility)}{FormatType(field.Type, options)} {field.Name}";
    
    private string FormatParameter(IParameterSymbol parameter, FormatOptions options) =>
        $"{FormatType(parameter.Type, options)} {parameter.Name}";

    private string FormatType(ISymbol? symbol, FormatOptions options)
    {
        if (symbol is ITypeSymbol typeSymbol && marker.IsMarker(typeSymbol))
        {
            return typeResolver.Resolve(typeSymbol).Name;
        }
        
        return symbol switch
        {
            INamedTypeSymbol { IsGenericType: true } namedTypeSymbol => $"{namedTypeSymbol.Name}{options.StartGenericArgsSymbol}{string.Join(options.TypeArgsSeparator, namedTypeSymbol.TypeArguments.Select(FormatSymbolLocal))}{options.FinishGenericArgsSymbol}",
            IArrayTypeSymbol array => $"Array{options.StartGenericArgsSymbol}{FormatType(array.ElementType, options)}{options.FinishGenericArgsSymbol}",
            _ => symbol?.Name ?? "Unresolved"
        };
        
        string FormatSymbolLocal(ITypeSymbol i) => FormatSymbol(i, options);
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

    private class ClassDiagramWalker(ClassDiagramBuilder builder, LinesBuilder lines, FormatOptions options)
        : DependenciesWalker<Unit>
    {
        private readonly LinesBuilder _nodeLines = new();

        public override void VisitDependencyNode(in Unit ctx, DependencyNode node)
        {
            base.VisitDependencyNode(ctx, node);
            if (!_nodeLines.Lines.Any())
            {
                lines.AppendLine($"class {builder.FormatType(node.Type, options)}");
                return;
            }

            lines.AppendLine($"class {builder.FormatType(node.Type, options)} {{");
            using (lines.Indent())
            {
                lines.AppendLines(_nodeLines.Lines);
            }
            
            lines.AppendLine("}");
        }

        public override void VisitConstructor(in Unit ctx, in DpMethod constructor)
        {
            _nodeLines.AppendLine(builder.FormatMethod(constructor.Method, options));
            base.VisitConstructor(ctx, in constructor);
        }

        public override void VisitMethod(in Unit ctx, in DpMethod method)
        {
            _nodeLines.AppendLine(builder.FormatMethod(method.Method, options));
            base.VisitMethod(ctx, in method);
        }

        public override void VisitProperty(in Unit ctx, in DpProperty property)
        {
            _nodeLines.AppendLine(builder.FormatProperty(property.Property, options));
            base.VisitProperty(ctx, in property);
        }

        public override void VisitField(in Unit ctx, in DpField field)
        {
            _nodeLines.AppendLine(builder.FormatField(field.Field, options));
            base.VisitField(ctx, in field);
        }
    }
}