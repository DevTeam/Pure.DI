// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI.Core.CSharp;

internal class ClassDiagramBuilder: IBuilder<CompositionCode, LinesBuilder>
{
    private readonly IBuilder<ContractsBuildContext, ISet<Injection>> _injectionsBuilder;
    private static readonly FormatOptions DefaultFormatOptions = new();
    
    public ClassDiagramBuilder(IBuilder<ContractsBuildContext, ISet<Injection>> injectionsBuilder)
    {
        _injectionsBuilder = injectionsBuilder;
    }

    public LinesBuilder Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var lines = new LinesBuilder();
        lines.AppendLine("classDiagram");
        using (lines.Indent())
        {
            var hasResolveMethods = composition.Source.Source.Settings.GetState(Setting.Resolve, SettingState.On) == SettingState.On;
            var publicRoots = composition.Roots.Where(i => i.IsPublic).ToDictionary(i => i.Injection, i => i);
            if (hasResolveMethods || publicRoots.Any())
            {
                lines.AppendLine($"class {composition.Name.ClassName} {{");
                using (lines.Indent())
                {
                    foreach (var root in composition.Roots.OrderByDescending(i => i.IsPublic).ThenBy(i => i.Name))
                    {
                        lines.AppendLine($"{(root.IsPublic ? "+" : "-")}{FormatType(root.Injection.Type, DefaultFormatOptions)} {root.PropertyName}");
                    }
                    
                    if (hasResolveMethods)
                    {
                        var genericParameterT = $"{DefaultFormatOptions.StartGenericArgsSymbol}T{DefaultFormatOptions.FinishGenericArgsSymbol}";
                        lines.AppendLine($"+T {Constant.ResolverMethodName}{genericParameterT}()");
                        lines.AppendLine($"+T {Constant.ResolverMethodName}{genericParameterT}(object? tag)");
                        lines.AppendLine($"+object {Constant.ResolverMethodName}{genericParameterT}(Type type)");
                        lines.AppendLine($"+object {Constant.ResolverMethodName}{genericParameterT}(Type type, object? tag)");
                    }
                }

                lines.AppendLine("}");
            }
            else
            {
                lines.AppendLine($"class {composition.Name.ClassName}");
            }

            if (composition.DisposableSingletonsCount > 0)
            {
                lines.AppendLine($"{composition.Name.ClassName} --|> IDisposable");
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
                
                var contracts = _injectionsBuilder.Build(new ContractsBuildContext(node.Binding, MdTag.ContextTag), CancellationToken.None);
                foreach (var contract in contracts)
                {
                    if (node.Type.Equals(contract.Type, SymbolEqualityComparer.Default))
                    {
                        continue;
                    }

                    types.Add(contract.Type);
                    lines.AppendLine($"{FormatType(node.Type, DefaultFormatOptions)} --|> {FormatType(contract.Type, DefaultFormatOptions)} : {FormatTag(contract.Tag)}");
                }

                var classDiagramWalker = new ClassDiagramWalker(lines, DefaultFormatOptions);
                classDiagramWalker.VisitDependencyNode(node);
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
                if (dependency.Target.Root is not null && publicRoots.TryGetValue(dependency.Injection, out var root))
                {
                    lines.AppendLine($"{composition.Name.ClassName} ..> {FormatType(dependency.Source.Type, DefaultFormatOptions)} : {FormatInjection(root.Injection, DefaultFormatOptions)} {root.PropertyName}");
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
                        var relationship = dependency.Source.Lifetime == Lifetime.Transient ? "*--" : "o--";
                        lines.AppendLine($"{FormatType(dependency.Target.Type, DefaultFormatOptions)} {relationship} {FormatCardinality(dependency.Source.Lifetime)} {FormatType(dependency.Source.Type, DefaultFormatOptions)} : {FormatDependency(dependency, DefaultFormatOptions)}");   
                    }
                }
            }
        }
        
        return lines;
    }

    private static string FormatCardinality(Lifetime lifetime) =>
        lifetime switch
        {
            Lifetime.Transient => "",
            _ => $" \\\"{lifetime}\\\""
        };

    private static string FormatInjection(Injection injection, FormatOptions options) => 
        $"{FormatTag(injection.Tag)}{FormatSymbol(injection.Type, options)}";

    private static string FormatDependency(Dependency dependency, FormatOptions options) => 
        $"{(dependency.Injection.Tag == default ? "" : FormatTag(dependency.Injection.Tag) + " ")}{FormatSymbol(dependency.TargetSymbol, options)}";

    private static string FormatTag(object? tag) =>
        tag != default ? $"{tag.ValueToString("").Replace("\"", "\\\"")} " : "";
    
    private static string FormatTags(IEnumerable<object?> tags) =>
        string.Join(", ", tags.Distinct().Select(FormatTag).OrderBy(i => i));

    private static string FormatSymbol(ISymbol? symbol, FormatOptions options) =>
        symbol switch
        {
            IParameterSymbol parameter => FormatParameter(parameter, options),
            IPropertySymbol property => FormatProperty(property, options),
            IFieldSymbol field => FormatField(field, options),
            IMethodSymbol method => FormatMethod(method, options),
            ITypeSymbol type => FormatType(type, options),
            _ => symbol?.ToString() ?? ""
        };

    private static string FormatMethod(IMethodSymbol method, FormatOptions options)
    {
        if (method is { Kind: SymbolKind.Property, ContainingSymbol: IPropertySymbol property })
        {
            return $"{Format(method.DeclaredAccessibility)}{property.Name} : {FormatType(method.ReturnType, options)}";
        }

        if (method.MethodKind == MethodKind.Constructor)
        {
            string FormatProperty(IParameterSymbol parameter) => $"{FormatType(parameter.Type, options)} {parameter.Name}";
            return $"{Format(method.DeclaredAccessibility)}{method.ContainingType.Name}({string.Join(", ", method.Parameters.Select(FormatProperty))})";
        }

        string FormatParameter(IParameterSymbol parameter) => $"{FormatType(parameter.Type, options)} {parameter.Name}";
        return $"{Format(method.DeclaredAccessibility)}{method.Name}({(string.Join(", ", method.Parameters.Select(FormatParameter)))}) : {FormatType(method.ReturnType, options)}";
    }

    private static string FormatProperty(IPropertySymbol property, FormatOptions options) =>
        $"{Format(property.GetMethod?.DeclaredAccessibility ?? property.DeclaredAccessibility)}{FormatType(property.Type, options)} {property.Name}";

    private static string FormatField(IFieldSymbol field, FormatOptions options) =>
        $"{Format(field.DeclaredAccessibility)}{FormatType(field.Type, options)} {field.Name}";
    
    private static string FormatParameter(IParameterSymbol parameter, FormatOptions options) =>
        $"{FormatType(parameter.Type, options)} {parameter.Name}";

    private static string FormatType(ISymbol typeSymbol, FormatOptions options)
    {
        string FormatSymbol(ITypeSymbol i) => ClassDiagramBuilder.FormatSymbol(i, options);
        return typeSymbol switch
        {
            INamedTypeSymbol { IsGenericType: true } namedTypeSymbol => $"{namedTypeSymbol.Name}{options.StartGenericArgsSymbol}{string.Join(options.TypeArgsSeparator, namedTypeSymbol.TypeArguments.Select(FormatSymbol))}{options.FinishGenericArgsSymbol}",
            IArrayTypeSymbol array => $"Array{options.StartGenericArgsSymbol}{FormatType(array.ElementType, options)}{options.FinishGenericArgsSymbol}",
            _ => typeSymbol.Name
        };
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

    private class ClassDiagramWalker : DependenciesWalker
    {
        private readonly LinesBuilder _lines;
        private readonly LinesBuilder _nodeLines = new();
        private readonly FormatOptions _options;

        public ClassDiagramWalker(LinesBuilder lines, FormatOptions options)
        {
            _lines = lines;
            _options = options;
        }

        public override void VisitDependencyNode(DependencyNode node)
        {
            base.VisitDependencyNode(node);
            if (!_nodeLines.Lines.Any())
            {
                _lines.AppendLine($"class {FormatType(node.Type, _options)}");
                return;
            }

            _lines.AppendLine($"class {FormatType(node.Type, _options)} {{");
            using (_lines.Indent())
            {
                _lines.AppendLines(_nodeLines.Lines);
            }
            
            _lines.AppendLine("}");
        }

        public override void VisitConstructor(in DpMethod constructor)
        {
            _nodeLines.AppendLine(FormatMethod(constructor.Method, _options));
            base.VisitConstructor(in constructor);
        }

        public override void VisitMethod(in DpMethod method)
        {
            _nodeLines.AppendLine(FormatMethod(method.Method, _options));
            base.VisitMethod(in method);
        }

        public override void VisitProperty(in DpProperty property)
        {
            _nodeLines.AppendLine(FormatProperty(property.Property, _options));
            base.VisitProperty(in property);
        }

        public override void VisitField(in DpField field)
        {
            _nodeLines.AppendLine(FormatField(field.Field, _options));
            base.VisitField(in field);
        }
    }
}