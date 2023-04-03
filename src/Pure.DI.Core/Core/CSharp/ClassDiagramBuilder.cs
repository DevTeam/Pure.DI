namespace Pure.DI.Core.CSharp;

internal class ClassDiagramBuilder: IBuilder<CompositionCode, LinesBuilder>
{
    private readonly IBuilder<MdBinding, ISet<Injection>> _injectionsBuilder;
    private static readonly FormatOptions DefaultFormatOptions = new();
    private static readonly FormatOptions CommentsFormatOptions = DefaultFormatOptions;

    public ClassDiagramBuilder(IBuilder<MdBinding, ISet<Injection>> injectionsBuilder)
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
                    foreach (var root in composition.Roots.Where(i => i.IsPublic))
                    {
                        lines.AppendLine($"+{FormatType(root.Injection.Type, DefaultFormatOptions)} {root.PropertyName}");
                    }
                }

                if (hasResolveMethods)
                {
                    var genericParameterT = $"{DefaultFormatOptions.StartGenericArgsSymbol}T{DefaultFormatOptions.FinishGenericArgsSymbol}";
                    lines.AppendLine($"+T {Constant.ResolverMethodName}{genericParameterT}()");
                    lines.AppendLine($"+T {Constant.ResolverMethodName}{genericParameterT}(object? tag)");
                    lines.AppendLine($"+object {Constant.ResolverMethodName}{genericParameterT}(Type type)");
                    lines.AppendLine($"+object {Constant.ResolverMethodName}{genericParameterT}(Type type, object? tag)");
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

            var graph = composition.Source.Graph;
            foreach (var node in graph.Vertices.GroupBy(i => i.Type, SymbolEqualityComparer.Default).Select(i => i.First()))
            {
                if (node.Root is { })
                {
                    continue;
                }
                
                var exposedInjections = _injectionsBuilder.Build(node.Binding, CancellationToken.None);
                foreach (var exposedInjection in exposedInjections)
                {
                    if (node.Type.Equals(exposedInjection.Type, SymbolEqualityComparer.Default))
                    {
                        continue;
                    }
                    
                    lines.AppendLine($"{FormatType(node.Type, DefaultFormatOptions)} --|> {FormatType(exposedInjection.Type, DefaultFormatOptions)} : {FormatTag(exposedInjection.Tag)}");
                }

                var classDiagramWalker = new ClassDiagramWalker(lines, DefaultFormatOptions);
                classDiagramWalker.VisitDependencyNode(node);
            }

            foreach (var dependency in graph.Edges)
            {
                if (dependency.Target.Root is {} && publicRoots.TryGetValue(dependency.Injection, out var root))
                {
                    lines.AppendLine($"{composition.Name.ClassName} ..> {FormatType(dependency.Source.Type, DefaultFormatOptions)} : {FormatInjection(root.Injection, CommentsFormatOptions)} {root.PropertyName}");
                }
                else
                {
                    if (dependency.Source.Arg is { } arg)
                    {
                        var tags = arg.Binding.Contracts.SelectMany(i => i.Tags.Select(tag => tag.Value)).ToArray();
                        lines.AppendLine($"{FormatType(dependency.Target.Type, DefaultFormatOptions)}{FormatCardinality(dependency.Target.Binding.Lifetime?.Lifetime)}o-- {FormatType(dependency.Source.Type, DefaultFormatOptions)} : {(tags.Any() ? FormatTags(tags) + " " : "")}Argument \\\"{arg.Source.ArgName}\\\"");
                    }
                    else
                    {
                        var relationship = dependency.Source.Lifetime == Lifetime.Transient ? "*--" : "o--";
                        lines.AppendLine($"{FormatType(dependency.Target.Type, DefaultFormatOptions)}{FormatCardinality(dependency.Target.Binding.Lifetime?.Lifetime)}{relationship}{FormatCardinality(dependency.Source.Binding.Lifetime?.Lifetime)}{FormatType(dependency.Source.Type, DefaultFormatOptions)} : {FormatDependency(dependency, CommentsFormatOptions)}");   
                    }
                }
            }
        }
        
        return lines;
    }

    private static string FormatCardinality(object? @object) =>
        @object switch
        {
            Lifetime.Transient => " ",
            not { } => " ",
            _ => $" \\\"{@object}\\\" "
        };

    private static string FormatInjection(Injection injection, FormatOptions options) => 
        $"{FormatTag(injection.Tag)}{FormatSymbol(injection.Type, options)}";

    private static string FormatDependency(Dependency dependency, FormatOptions options) => 
        $"{(dependency.Injection.Tag == default ? "" : FormatTag(dependency.Injection.Tag) + " ")}{FormatSymbol(dependency.TargetSymbol, options)}";

    private static string FormatTag(object? tag) =>
        tag != default ? $"{tag.TagToString("").Replace("\"", "\\\"")} " : "";
    
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
            return $"{Format(method.DeclaredAccessibility)}{method.ContainingType.Name}({(string.Join(", ", method.Parameters.Select(parameter => $"{FormatType(parameter.Type, options)} {parameter.Name}")))})"; 
        }
        
        return $"{Format(method.DeclaredAccessibility)}{method.Name}({(string.Join(", ", method.Parameters.Select(parameter => $"{FormatType(parameter.Type, options)} {parameter.Name}")))}) : {FormatType(method.ReturnType, options)}";
    }

    private static string FormatProperty(IPropertySymbol property, FormatOptions options) =>
        $"{Format(property.GetMethod?.DeclaredAccessibility ?? property.DeclaredAccessibility)}{FormatType(property.Type, options)} {property.Name}";

    private static string FormatField(IFieldSymbol field, FormatOptions options) =>
        $"{Format(field.DeclaredAccessibility)}{FormatType(field.Type, options)} {field.Name}";
    
    private static string FormatParameter(IParameterSymbol parameter, FormatOptions options) =>
        $"{FormatType(parameter.Type, options)} {parameter.Name}";

    private static string FormatType(ISymbol typeSymbol, FormatOptions options) =>
        typeSymbol switch
        {
            INamedTypeSymbol { IsGenericType: true } namedTypeSymbol => $"{namedTypeSymbol.Name}{options.StartGenericArgsSymbol}{string.Join(",", namedTypeSymbol.TypeArguments.Select(i => FormatSymbol(i, options)))}{options.FinishGenericArgsSymbol}",
            IArrayTypeSymbol array => $"Array{options.StartGenericArgsSymbol}{FormatType(array.ElementType, options)}{options.FinishGenericArgsSymbol}",
            _ => typeSymbol.Name
        };

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
        char StartGenericArgsSymbol = 'ᐸ',
        char FinishGenericArgsSymbol = 'ᐳ');

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
                _lines.AppendLines(_nodeLines);
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