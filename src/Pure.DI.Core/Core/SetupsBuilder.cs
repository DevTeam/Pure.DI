// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.

namespace Pure.DI.Core;

sealed class SetupsBuilder(
    Func<IMetadataWalker> metadataSyntaxWalkerFactory,
    ICache<int, bool> setupCache,
    Func<IBindingBuilder> bindingBuilderFactory,
    IArguments arguments,
    Func<ITypeConstructor> typeConstructorFactory,
    ISemantic semantic,
    ISymbolNames symbolNames,
    ILogger logger,
    IMarker marker,
    MarkerTypeRewriter markerTypeRewriter,
    Func<ILocalVariableRenamingRewriter> localVariableRenamingRewriterFactory,
    IRegistryManager<int> bindingsRegistryManager,
    IEqualityComparer<(ITypeSymbol ContractType, object? Tag)> contractTagComparer,
    ITypeSymbolComparer typeSymbolComparer)
    : IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>, IMetadataVisitor, ISetupFinalizer
{
    private sealed record TypeAttributeBinding(
        ITypeSymbol Type,
        ImmutableArray<ITypeSymbol> Contracts,
        ImmutableArray<object?> Tags,
        Lifetime? Lifetime);

    private readonly List<MdAccumulator> _accumulators = [];
    private readonly List<MdBinding> _bindings = [];
    private readonly List<MdDependsOn> _dependsOn = [];
    private readonly List<MdGenericTypeArgumentAttribute> _genericTypeArgumentAttributes = [];
    private readonly List<MdGenericTypeArgument> _genericTypeArguments = [];
    private readonly List<MdOrdinalAttribute> _ordinalAttributes = [];
    private readonly List<MdSpecialType> _specialTypes = [];
    private readonly List<MdRoot> _roots = [];
    private readonly List<MdSetup> _setups = [];
    private readonly List<SetupContextMembers> _setupContextMembers = [];
    private readonly List<MdLifetimeAttribute> _lifetimeAttributes = [];
    private readonly List<MdTagAttribute> _tagAttributes = [];
    private readonly List<MdTypeAttribute> _typeAttributes = [];
    private readonly List<MdUsingDirectives> _usingDirectives = [];
    private readonly List<IBindingBuilder> _bindingBuilders = [];
    private readonly List<MdDefaultLifetime> _defaultLifetimes = [];
    private IBindingBuilder _bindingBuilder = bindingBuilderFactory();
    private Hints _hints = new();
    private MdSetup? _setup;
    private MdDefaultLifetime? _defaultLifetime;

    public IEnumerable<MdSetup> Build(SyntaxUpdate update)
    {
        var checkSum = update.Node.SyntaxTree.GetText().GetChecksum();
        var checkSumKey = 0;
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var checkSumItem in checkSum)
        {
            checkSumKey = checkSumKey * 397 ^ checkSumItem;
        }

        if (!setupCache.Get(checkSumKey, _ => true))
        {
            return [];
        }

        metadataSyntaxWalkerFactory().Visit(this, update);
        if (_setups.Count == 0)
        {
            setupCache.Set(checkSumKey, false);
        }

        return _setups;
    }

    public void VisitSetup(in MdSetup setup)
    {
        FinishSetup(_setup);
        _setup = setup;
    }

    public void VisitUsingDirectives(in MdUsingDirectives usingDirectives) =>
        _usingDirectives.Add(usingDirectives);

    public void VisitBinding(in MdBinding binding)
    {
    }

    public void VisitContract(in MdContract contract) =>
        _bindingBuilder.AddContract(contract);

    public void VisitImplementation(in MdImplementation implementation)
    {
        _bindingBuilder.Implementation = implementation;
        FinishBinding();
    }

    public void VisitFactory(in MdFactory factory)
    {
        _bindingBuilder.Factory = factory;
        FinishBinding();
    }

    public void VisitArg(in MdArg arg)
    {
        _bindingBuilder.Arg = arg;
        FinishBinding();
    }

    public void VisitResolver(in MdResolver resolver)
    {
    }

    public void VisitInitializer(MdInitializer initializer)
    {
    }

    public void VisitRoot(in MdRoot root) => _roots.Add(root);

    public void VisitGenericTypeArgument(in MdGenericTypeArgument genericTypeArgument) =>
        _genericTypeArguments.Add(genericTypeArgument);

    public void VisitGenericTypeArgumentAttribute(in MdGenericTypeArgumentAttribute genericTypeArgumentAttribute) =>
        _genericTypeArgumentAttributes.Add(genericTypeArgumentAttribute);

    public void VisitDefaultLifetime(in MdDefaultLifetime defaultLifetime)
    {
        _defaultLifetime = defaultLifetime;
        _defaultLifetimes.Add(defaultLifetime);
        _bindingBuilder.AddDefaultLifetime(defaultLifetime);
    }

    public void VisitDependsOn(in MdDependsOn dependsOn) =>
        _dependsOn.Add(dependsOn);

    public void VisitTypeAttribute(in MdTypeAttribute typeAttribute) =>
        _typeAttributes.Add(typeAttribute);

    public void VisitTagAttribute(in MdTagAttribute tagAttribute) =>
        _tagAttributes.Add(tagAttribute);

    public void VisitLifetimeAttribute(in MdLifetimeAttribute lifetimeAttribute) =>
        _lifetimeAttributes.Add(lifetimeAttribute);

    public void VisitOrdinalAttribute(in MdOrdinalAttribute ordinalAttribute) =>
        _ordinalAttributes.Add(ordinalAttribute);

    public void VisitSpecialType(in MdSpecialType specialType) =>
        _specialTypes.Add(specialType);

    public void VisitLifetime(in MdLifetime lifetime) =>
        _bindingBuilder.Lifetime = lifetime;

    public void VisitTag(in MdTag tag) => _bindingBuilder.AddTag(tag);

    public void VisitAccumulator(in MdAccumulator accumulator) =>
        _accumulators.Add(accumulator);

    public void VisitHint(in MdHint hint)
    {
        var values = _hints.GetOrAdd(hint.Key, _ => new LinkedList<string>());
        foreach (var value in hint.Values)
        {
            values.AddLast(value);
        }
    }

    public void VisitFinish() => FinishSetup(_setup);

    public MdSetup Finalize(MdSetup setup, IReadOnlyDictionary<CompositionName, MdSetup> setupMap)
    {
        _setup = setup;
        _hints = new Hints();
        foreach (var item in setup.Hints)
        {
            _hints[item.Key] = item.Value;
        }

        _bindings.AddRange(setup.Bindings);
        _roots.AddRange(setup.Roots.Select(i => ConfigureRoot(i, _hints)));
        _dependsOn.AddRange(setup.DependsOn);
        _genericTypeArguments.AddRange(setup.GenericTypeArguments);
        _genericTypeArgumentAttributes.AddRange(setup.GenericTypeArgumentAttributes);
        _typeAttributes.AddRange(setup.TypeAttributes);
        _tagAttributes.AddRange(setup.TagAttributes);
        _lifetimeAttributes.AddRange(setup.LifetimeAttributes);
        _ordinalAttributes.AddRange(setup.OrdinalAttributes);
        _specialTypes.AddRange(setup.SpecialTypes);
        _usingDirectives.AddRange(setup.UsingDirectives);
        _accumulators.AddRange(setup.Accumulators);
        _defaultLifetimes.AddRange(setup.DefaultLifetimes);
        if (!setup.SetupContextMembers.IsDefaultOrEmpty)
        {
            _setupContextMembers.AddRange(setup.SetupContextMembers);
        }

        AddTypeAttributeBindings(setup);

        foreach (var binding in setup.Bindings)
        {
            FinalizeBinding(setup, setupMap, binding);
        }

        return FinishSetup(null)!;
    }

    private void FinishBinding()
    {
        _bindingBuilders.Add(_bindingBuilder);
        _bindingBuilder = bindingBuilderFactory();
        if (_defaultLifetime is {} defaultLifetime)
        {
            _bindingBuilder.AddDefaultLifetime(defaultLifetime);
        }
    }

    private void FinalizeBinding(MdSetup setup, IReadOnlyDictionary<CompositionName, MdSetup> setupMap, MdBinding binding)
    {
        ITypeSymbol type;
        if (binding.Arg is {} arg)
        {
            type = arg.Type;
        }
        else
        {
            if (binding.Implementation is {} implementation)
            {
                type = implementation.Type;
            }
            else
            {
                if (binding.Factory is {} factory)
                {
                    type = factory.Type;
                }
                else
                {
                    return;
                }
            }
        }

        var source = binding.Source;
        var semanticModel = binding.SemanticModel;
        if (binding.Contracts.IsDefaultOrEmpty)
        {
            return;
        }

        var contacts = binding.Contracts.Where(i => i.ContractType is not null).ToList();
        if (contacts.Count == 0)
        {
            return;
        }

        var contract = contacts.First();

        var membersToBind = (
            from member in type.GetMembers()
            where member.DeclaredAccessibility >= Accessibility.Internal
                  && member.CanBeReferencedByName
                  && member is IFieldSymbol or IPropertySymbol or IMethodSymbol
            from attribute in member.GetAttributes()
            where attribute.AttributeClass is {} attributeClass
                  && symbolNames.GetGlobalName(attributeClass) == Names.ExportAttributeName
            select (member, attribute.ConstructorArguments, attribute.NamedArguments))
            .ToList();

        var className = type is INamedTypeSymbol namedTypeSymbol
            ? GetTypeName(namedTypeSymbol)
            : type.Name;

        var typeNamespace = "";
        if (type.ContainingNamespace is { IsGlobalNamespace: false } ns)
        {
            typeNamespace = ns.ToDisplayString();
        }

        var name = new CompositionName(className, typeNamespace, null);

        var exportedRoots = ImmutableArray<MdRoot>.Empty;
        if (setupMap.TryGetValue(name, out var boundSetup))
        {
            exportedRoots = boundSetup.Roots.Where(i => (i.Kind & RootKinds.Exported) == RootKinds.Exported).ToImmutableArray();
        }

        if (membersToBind.Count == 0 && exportedRoots.Length == 0)
        {
            return;
        }

        bindingsRegistryManager.Register(setup, binding.Id);
        var typeConstructor = typeConstructorFactory();
        var typeParameterMap = CreateTypeParameterMap(type);
        var boundMemberContracts = new HashSet<(ITypeSymbol ContractType, object? Tag)>(contractTagComparer);
        foreach (var (member, constructorArguments, namedArguments) in membersToBind)
        {
            var values = arguments.GetArgs(constructorArguments, namedArguments, "type", "lifetime", "tags");
            ITypeSymbol? contractType = null;
            ITypeSymbol? memberType;
            if (values.Length > 0 && values[0].Value is ITypeSymbol newContractType)
            {
                contractType = newContractType;
            }

            var position = 0;
            var namespaces = new List<string>();
            var resolvers = new List<MdResolver>();
            switch (member)
            {
                case IFieldSymbol fieldSymbol:
                    contractType ??= fieldSymbol.Type;
                    memberType = fieldSymbol.Type;
                    break;

                case IPropertySymbol propertySymbol:
                    contractType ??= propertySymbol.Type;
                    memberType = propertySymbol.Type;
                    break;

                case IMethodSymbol methodSymbol:
                    contractType ??= methodSymbol.ReturnType;
                    memberType = methodSymbol.ReturnType;
                    if (methodSymbol.IsGenericMethod)
                    {
                        typeConstructor.TryBind(setup, contractType, methodSymbol.ReturnType);
                        contractType = typeConstructor.ConstructReversed(contractType);

                        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                        foreach (var parameter in methodSymbol.Parameters)
                        {
                            var paramType = typeConstructor.ConstructReversed(parameter.Type);
                            resolvers.Add(CreateResolver(typeConstructor, parameter.Name, paramType, MdTag.ContextTag, ref position));
                        }
                    }
                    else
                    {
                        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                        foreach (var parameter in methodSymbol.Parameters)
                        {
                            resolvers.Add(CreateResolver(typeConstructor, parameter.Name, parameter.Type, MdTag.ContextTag, ref position));
                        }
                    }

                    break;

                default:
                    continue;
            }

            if (member is not IMethodSymbol { IsGenericMethod: true })
            {
                typeConstructor.TryBind(setup, contractType, memberType);
                contractType = typeConstructor.Construct(setup, contractType);
            }

            contractType = SubstituteTypeParameters(contractType, typeParameterMap);

            var lifetime = Lifetime.Transient;
            if (values.Length > 1 && values[1].Value is int newLifetime)
            {
                lifetime = (Lifetime)newLifetime;
            }

            List<object?> tags;
            if (values.Length > 2)
            {
                var tagsValue = values[2];
                if (tagsValue.Kind == TypedConstantKind.Array)
                {
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (!tagsValue.Values.IsDefaultOrEmpty)
                    {
                        tags = tagsValue.Values.Select(i => i.Value).ToList();
                    }
                    else
                    {
                        tags = [];
                    }
                }
                else
                {
                    tags = values.Skip(2).Select(i => i.Value).ToList();
                }
            }
            else
            {
                tags = [];
            }

            TrackMemberBindings(contractType, tags);

            object? valueTag = null;
            if (!contract.Tags.IsDefaultOrEmpty)
            {
                valueTag = contract.Tags.First().Value;
            }

            if (!member.IsStatic)
            {
                resolvers.Add(CreateResolver(typeConstructor, Names.DefaultInstanceValueName, contract.ContractType!, valueTag, ref position));
            }

            VisitContract(
                new MdContract(
                    semanticModel,
                    source,
                    contractType,
                    ContractKind.Explicit,
                    ImmutableArray<MdTag>.Empty));

            if (lifetime != Lifetime.Transient)
            {
                VisitLifetime(new MdLifetime(semanticModel, source, lifetime));
            }

            var tagPosition = 0;
            foreach (var tag in tags)
            {
                VisitTag(new MdTag(tagPosition++, tag));
            }

            if (tags.Count == 0)
            {
                VisitTag(new MdTag(tagPosition, null));
            }

            var memberResolver = CreateResolver(typeConstructor, Names.DefaultInstanceValueName, contract.ContractType!, valueTag, ref position);
            memberResolver = memberResolver with { Member = member };

            LambdaExpressionSyntax factoryExpression;
            if (!member.IsStatic)
            {
                factoryExpression = SyntaxFactory.SimpleLambdaExpression(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(Names.DefaultInstanceValueName)),
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(Names.DefaultInstanceValueName),
                        SyntaxFactory.IdentifierName(member.Name)));
            }
            else
            {
                factoryExpression = SyntaxFactory.ParenthesizedLambdaExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParseTypeName(symbolNames.GetGlobalName(type)),
                        SyntaxFactory.IdentifierName(member.Name)));
            }

            VisitFactory(
                new MdFactory(
                    semanticModel,
                    source,
                    contractType,
                    localVariableRenamingRewriterFactory(),
                    factoryExpression,
                    true,
                    RootBuilder.DefaultCtxParameter,
                    resolvers.ToImmutableArray(),
                    ImmutableArray<MdInitializer>.Empty,
                    false,
                    memberResolver));

            VisitUsingDirectives(new MdUsingDirectives(namespaces, [], []));
            continue;

            MdResolver CreateResolver(ITypeConstructor constructor, string paramName, ITypeSymbol injectedType, object? tag, ref int curPosition)
            {
                var typeSyntax = SyntaxFactory.ParseTypeName(symbolNames.GetGlobalName(injectedType));
                var injectedNamespace = injectedType.ContainingNamespace;
                if (injectedNamespace is { IsGlobalNamespace: false } && semantic.IsValidNamespace(injectedNamespace))
                {
                    namespaces.Add(injectedNamespace.ToString());
                }

                return new MdResolver
                {
                    SemanticModel = semanticModel,
                    Source = source,
                    ContractType = injectedType,
                    Tag = new MdTag(curPosition, tag),
                    ArgumentType = typeSyntax,
                    Parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName)).WithType(typeSyntax),
                    Position = curPosition++,
                    TypeConstructor = constructor
                };
            }
        }

        // Adds bindings for exported roots from related compositions.
        // At this stage, exported root members may not exist in semantic model yet, so we bind by member name.
        foreach (var root in exportedRoots)
        {
            if (string.IsNullOrWhiteSpace(root.Name))
            {
                continue;
            }

            var rootTagValue = root.Tag?.Value;
            if (rootTagValue == MdTag.ContextTag)
            {
                rootTagValue = null;
            }

            var rootContractType = SubstituteTypeParameters(root.RootContractType, typeParameterMap);
            rootContractType = markerTypeRewriter.ReplaceTypeParametersWithMarkers(semanticModel, rootContractType);
            if (boundMemberContracts.Contains((rootContractType, rootTagValue)))
            {
                continue;
            }

            var tags = new List<object?>();
            if (root.Tag is { } rootTag && rootTag.Value != MdTag.ContextTag)
            {
                tags.Add(rootTag.Value);
            }

            object? valueTag = null;
            if (!contract.Tags.IsDefaultOrEmpty)
            {
                valueTag = contract.Tags.First().Value;
            }

            var compositionContractType = contract.ContractType;
            if (compositionContractType is null)
            {
                continue;
            }

            var position = 0;
            var namespaces = new List<string>();
            var resolvers = new List<MdResolver>();

            var isStatic = (root.Kind & RootKinds.Static) == RootKinds.Static;
            if (!isStatic)
            {
                resolvers.Add(CreateExportedResolver(typeConstructor, Names.DefaultInstanceValueName, compositionContractType, valueTag, ref position, namespaces));
            }

            VisitContract(
                new MdContract(
                    semanticModel,
                    source,
                    rootContractType,
                    ContractKind.Explicit,
                    ImmutableArray<MdTag>.Empty));

            VisitLifetime(new MdLifetime(semanticModel, source, Lifetime.Transient));

            var tagPosition = 0;
            foreach (var tag in tags)
            {
                VisitTag(new MdTag(tagPosition++, tag));
            }

            if (tags.Count == 0)
            {
                VisitTag(new MdTag(tagPosition, null));
            }

            ExpressionSyntax instanceExpr = isStatic
                ? SyntaxFactory.ParseTypeName(symbolNames.GetGlobalName(compositionContractType))
                : SyntaxFactory.IdentifierName(Names.DefaultInstanceValueName);

            var factoryExpr = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                instanceExpr,
                SyntaxFactory.IdentifierName(root.Name));

            LambdaExpressionSyntax factoryLambda = SyntaxFactory.ParenthesizedLambdaExpression(factoryExpr);
            if (!isStatic)
            {
                factoryLambda = SyntaxFactory.SimpleLambdaExpression(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(Names.DefaultInstanceValueName)),
                    factoryExpr);
            }

            var memberResolver = CreateExportedResolver(typeConstructor, Names.DefaultInstanceValueName, compositionContractType, valueTag, ref position, namespaces);
            memberResolver = memberResolver with { MemberName = root.Name };

            VisitFactory(
                new MdFactory(
                    semanticModel,
                    source,
                    rootContractType,
                    localVariableRenamingRewriterFactory(),
                    factoryLambda,
                    true,
                    RootBuilder.DefaultCtxParameter,
                    resolvers.ToImmutableArray(),
                    ImmutableArray<MdInitializer>.Empty,
                    false,
                    memberResolver));

            VisitUsingDirectives(new MdUsingDirectives(namespaces, [], []));
        }

        return;

        MdResolver CreateExportedResolver(
            ITypeConstructor constructor,
            string paramName,
            ITypeSymbol injectedType,
            object? tag,
            ref int curPosition,
            List<string> namespaces)
        {
            var typeSyntax = SyntaxFactory.ParseTypeName(symbolNames.GetGlobalName(injectedType));
            var injectedNamespace = injectedType.ContainingNamespace;
            if (injectedNamespace is { IsGlobalNamespace: false } && semantic.IsValidNamespace(injectedNamespace))
            {
                namespaces.Add(injectedNamespace.ToString());
            }

            return new MdResolver
            {
                SemanticModel = semanticModel,
                Source = source,
                ContractType = injectedType,
                Tag = new MdTag(curPosition, tag),
                ArgumentType = typeSyntax,
                Parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName)).WithType(typeSyntax),
                Position = curPosition++,
                TypeConstructor = constructor
            };
        }

        Dictionary<ITypeParameterSymbol, ITypeSymbol> CreateTypeParameterMap(ITypeSymbol compositionType)
        {
            if (compositionType is not INamedTypeSymbol { IsGenericType: true } namedType)
            {
                return new Dictionary<ITypeParameterSymbol, ITypeSymbol>(typeSymbolComparer.Runtime);
            }

            var definition = namedType.OriginalDefinition;
            if (definition.TypeParameters.Length != namedType.TypeArguments.Length)
            {
                return new Dictionary<ITypeParameterSymbol, ITypeSymbol>(typeSymbolComparer.Runtime);
            }

            var map = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(typeSymbolComparer.Runtime);
            for (var i = 0; i < definition.TypeParameters.Length; i++)
            {
                map[definition.TypeParameters[i]] = namedType.TypeArguments[i];
            }

            return map;
        }

        string GetTypeName(INamedTypeSymbol typeSymbol)
        {
            var parts = new Stack<string>();
            var current = typeSymbol.OriginalDefinition;
            while (current != null)
            {
                var typeName = current.Name;
                if (current.TypeParameters.Length > 0)
                {
                    typeName = $"{typeName}<{string.Join(", ", current.TypeParameters.Select(i => i.Name))}>";
                }

                parts.Push(typeName);
                current = current.ContainingType?.OriginalDefinition;
            }

            return string.Join(".", parts);
        }

        ITypeSymbol SubstituteTypeParameters(ITypeSymbol typeSymbol, IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> map)
        {
            if (map.Count == 0)
            {
                return typeSymbol;
            }

            switch (typeSymbol)
            {
                case ITypeParameterSymbol typeParameter when map.TryGetValue(typeParameter, out var mappedType):
                    return mappedType;

                case INamedTypeSymbol { IsGenericType: true } namedType:
                {
                    var args = namedType.TypeArguments.Select(i => SubstituteTypeParameters(i, map)).ToArray();
                    var constructed = namedType.OriginalDefinition.Construct(args);
                    return constructed.WithNullableAnnotation(namedType.NullableAnnotation);
                }

                case IArrayTypeSymbol arrayType:
                {
                    var elementType = SubstituteTypeParameters(arrayType.ElementType, map);
                    var result = semanticModel.Compilation.CreateArrayTypeSymbol(elementType, arrayType.Rank);
                    return result.WithNullableAnnotation(arrayType.NullableAnnotation);
                }

                default:
                    return typeSymbol;
            }

        }

        void TrackMemberBindings(ITypeSymbol? memberContractType, List<object?> memberTags)
        {
            if (memberContractType is null)
            {
                return;
            }

            if (memberTags.Count == 0)
            {
                boundMemberContracts.Add((memberContractType, null));
                return;
            }

            foreach (var normalizedTag in memberTags.Select(tag => tag == MdTag.ContextTag ? null : tag))
            {
                boundMemberContracts.Add((memberContractType, normalizedTag));
            }
        }
    }

    private MdSetup? FinishSetup(MdSetup? source)
    {
        if (_setup is not {} setup)
        {
            return null;
        }

        setup = setup with
        {
            Hints = _hints,
            Roots = _roots.ToImmutableArray(),
            DependsOn = _dependsOn.ToImmutableArray(),
            GenericTypeArguments = _genericTypeArguments.ToImmutableArray(),
            GenericTypeArgumentAttributes = _genericTypeArgumentAttributes.ToImmutableArray(),
            TypeAttributes = _typeAttributes.ToImmutableArray(),
            TagAttributes = _tagAttributes.ToImmutableArray(),
            LifetimeAttributes = _lifetimeAttributes.ToImmutableArray(),
            OrdinalAttributes = _ordinalAttributes.ToImmutableArray(),
            SpecialTypes = _specialTypes.ToImmutableArray(),
            UsingDirectives = _usingDirectives.ToImmutableArray(),
            Accumulators = _accumulators.Distinct().ToImmutableArray(),
            SetupContextMembers = _setupContextMembers.ToImmutableArray(),
            DefaultLifetimes = _defaultLifetimes.ToImmutableArray()
        };

        // Creates bindings with all relevant information.
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var bindingBuilder in _bindingBuilders)
        {
            var binding = bindingBuilder.Build(setup);
            binding = binding with { SourceSetup = source ?? binding.SourceSetup };
            _bindings.Add(binding);
        }

        setup = setup with { Bindings = _bindings.Select(i => i with { SourceSetup = source ?? i.SourceSetup }).ToImmutableArray() };

        _setups.Add(setup);
        _hints = new Hints();
        _bindingBuilders.Clear();
        _bindings.Clear();
        _roots.Clear();
        _setupContextMembers.Clear();
        _dependsOn.Clear();
        _genericTypeArguments.Clear();
        _genericTypeArgumentAttributes.Clear();
        _typeAttributes.Clear();
        _lifetimeAttributes.Clear();
        _ordinalAttributes.Clear();
        _specialTypes.Clear();
        _usingDirectives.Clear();
        _accumulators.Clear();
        _defaultLifetimes.Clear();
        _setup = null;
        _bindingBuilder = bindingBuilderFactory();
        return setup;
    }

    private void AddTypeAttributeBindings(MdSetup setup)
    {
        var compilation = setup.SemanticModel.Compilation;
        var processedTypes = new HashSet<ITypeSymbol>(typeSymbolComparer.Runtime);
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();
            foreach (var typeDeclaration in root
                         .DescendantNodes()
                         .OfType<TypeDeclarationSyntax>()
                         .Where(typeDeclaration => (typeDeclaration is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax)
                                                   && typeDeclaration.AttributeLists.Count > 0))
            {
                if (semanticModel.GetDeclaredSymbol(typeDeclaration) is not ITypeSymbol type || !processedTypes.Add(type))
                {
                    continue;
                }

                foreach (var binding in GetTypeAttributeBindings(setup, semanticModel, type))
                {
                    var builder = bindingBuilderFactory();
                    foreach (var defaultLifetime in setup.DefaultLifetimes)
                    {
                        builder.AddDefaultLifetime(defaultLifetime);
                    }

                    foreach (var contract in binding.Contracts)
                    {
                        builder.AddContract(
                            new MdContract(
                                semanticModel,
                                setup.Source,
                                contract,
                                ContractKind.Explicit,
                                ImmutableArray<MdTag>.Empty));
                    }

                    if (binding.Contracts.IsDefaultOrEmpty)
                    {
                        builder.AddContract(
                            new MdContract(
                                semanticModel,
                                setup.Source,
                                null,
                                ContractKind.Explicit,
                                binding.Tags.Select((tag, index) => new MdTag(index, tag)).ToImmutableArray()));
                    }

                    if (binding.Lifetime is {} lifetime)
                    {
                        builder.Lifetime = new MdLifetime(semanticModel, setup.Source, lifetime);
                    }

                    for (var tagIndex = 0; tagIndex < binding.Tags.Length; tagIndex++)
                    {
                        builder.AddTag(new MdTag(tagIndex, binding.Tags[tagIndex]));
                    }

                    builder.Implementation = new MdImplementation(semanticModel, setup.Source, binding.Type);
                    var mdBinding = builder.Build(setup);
                    bindingsRegistryManager.Register(setup, mdBinding.Id);
                    _bindings.Add(mdBinding);
                }
            }
        }
    }

    private IEnumerable<TypeAttributeBinding> GetTypeAttributeBindings(
        MdSetup setup,
        SemanticModel semanticModel,
        ITypeSymbol type)
    {
        var customBindings = new List<TypeAttributeBinding>();
        var hasTypeAttributes = false;
        foreach (var attributeGroup in GetAttributeGroups(type))
        {
            var groupBindings = new List<TypeAttributeBinding>();
            var groupHasTypeAttributes = false;
            foreach (var attributeData in attributeGroup)
            {
                if (attributeData.AttributeClass is not {} attributeClass)
                {
                    continue;
                }

                var typeAttributes = setup.TypeAttributes
                    .Where(attribute => IsAttributeMatch(attributeClass, attribute.AttributeType))
                    .ToImmutableArray();
                var tagAttributes = setup.TagAttributes
                    .Where(attribute => IsAttributeMatch(attributeClass, attribute.AttributeType))
                    .ToImmutableArray();
                var lifetimeAttributes = setup.LifetimeAttributes
                    .Where(attribute => IsAttributeMatch(attributeClass, attribute.AttributeType))
                    .ToImmutableArray();

                var isBindingAttribute = !typeAttributes.IsDefaultOrEmpty
                    || !tagAttributes.IsDefaultOrEmpty
                    || !lifetimeAttributes.IsDefaultOrEmpty;
                if (!isBindingAttribute)
                {
                    continue;
                }

                var contracts = typeAttributes
                    .Select(attribute => GetTypeAttributeValue(attributeData, attribute.ArgumentPosition))
                    .Where(contract => contract is not null)
                    .Select(contract => contract!)
                    .ToImmutableArray();

                var tags = tagAttributes
                    .SelectMany(attribute => GetTagAttributeValues(attributeData, attribute.ArgumentPosition))
                    .ToImmutableArray();

                var lifetime = lifetimeAttributes
                    .Select(attribute => GetLifetimeAttributeValue(attributeData, attribute.ArgumentPosition))
                    .FirstOrDefault(lifetime => lifetime is not null);

                groupBindings.Add(new TypeAttributeBinding(type, contracts, tags, lifetime));
                groupHasTypeAttributes |= !typeAttributes.IsDefaultOrEmpty;
            }

            if (groupBindings.Count == 0)
            {
                continue;
            }

            if (customBindings.Count > 0 && hasTypeAttributes && groupHasTypeAttributes)
            {
                foreach (var customBinding in GetMergedTypeAttributeBindings(setup, type, customBindings))
                {
                    yield return customBinding;
                }

                customBindings.Clear();
                hasTypeAttributes = false;
            }

            customBindings.AddRange(groupBindings);
            hasTypeAttributes |= groupHasTypeAttributes;
        }

        foreach (var customBinding in GetMergedTypeAttributeBindings(setup, type, customBindings))
        {
            yield return customBinding;
        }
    }

    private IEnumerable<TypeAttributeBinding> GetMergedTypeAttributeBindings(
        MdSetup setup,
        ITypeSymbol type,
        IReadOnlyCollection<TypeAttributeBinding> customBindings)
    {
        foreach (var customBinding in MergeTypeAttributeBindings(type, customBindings))
        {
            yield return customBinding with
            {
                Type = GetImplementationType(setup, type, customBinding.Contracts)
            };
        }
    }

    private static IEnumerable<IReadOnlyCollection<AttributeData>> GetAttributeGroups(ITypeSymbol type)
    {
        var currentAttributes = new List<AttributeData>();
        AttributeListSyntax? currentAttributeList = null;
        foreach (var attributeData in type.GetAttributes())
        {
            var attributeList = GetAttributeList(attributeData);
            if (currentAttributes.Count > 0 && !ReferenceEquals(currentAttributeList, attributeList))
            {
                yield return currentAttributes;
                currentAttributes = [];
            }

            currentAttributeList = attributeList;
            currentAttributes.Add(attributeData);
        }

        if (currentAttributes.Count > 0)
        {
            yield return currentAttributes;
        }
    }

    private static AttributeListSyntax? GetAttributeList(AttributeData attributeData) =>
        attributeData.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attribute
            ? attribute.Parent as AttributeListSyntax
            : null;

    private ITypeSymbol GetImplementationType(MdSetup setup, ITypeSymbol type, ImmutableArray<ITypeSymbol> contracts)
    {
        if (type is not INamedTypeSymbol { IsGenericType: true } implementationType)
        {
            return type;
        }

        foreach (var contract in contracts.Where(contract => marker.IsMarkerBased(setup, contract)))
        {
            foreach (var implementedContract in GetImplementedContracts(implementationType))
            {
                var typeConstructor = typeConstructorFactory();
                if (typeConstructor.TryBind(setup, contract, implementedContract))
                {
                    return typeConstructor.ConstructReversed(implementationType);
                }
            }
        }

        return type;
    }

    private static IEnumerable<ITypeSymbol> GetImplementedContracts(INamedTypeSymbol implementationType)
    {
        yield return implementationType;

        var baseType = implementationType.BaseType;
        while (baseType is not null)
        {
            yield return baseType;
            baseType = baseType.BaseType;
        }

        foreach (var implementedInterface in implementationType.AllInterfaces)
        {
            yield return implementedInterface;
        }
    }

    private IEnumerable<TypeAttributeBinding> MergeTypeAttributeBindings(
        ITypeSymbol type,
        IReadOnlyCollection<TypeAttributeBinding> bindings)
    {
        if (bindings.Count == 0)
        {
            yield break;
        }

        var lifetimes = bindings
            .Select(binding => binding.Lifetime)
            .OfType<Lifetime>()
            .ToArray();
        if (lifetimes.Length > 1)
        {
            logger.CompileError(
                new LogMessage(
                    string.Format(Strings.Error_Template_MultipleBindingLifetimes, type),
                    nameof(Strings.Error_Template_MultipleBindingLifetimes)),
                type.Locations.Where(location => location != Location.None).ToImmutableArray(),
                LogId.ErrorInvalidBinding);
            yield break;
        }

        var contracts = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var tags = ImmutableArray.CreateBuilder<object?>();
        foreach (var binding in bindings)
        {
            foreach (var contract in binding.Contracts)
            {
                if (!contracts.Any(item => typeSymbolComparer.RuntimeEquals(item, contract)))
                {
                    contracts.Add(contract);
                }
            }

            foreach (var tag in binding.Tags)
            {
                if (!tags.Any(item => TagEquals(item, tag)))
                {
                    tags.Add(tag);
                }
            }
        }

        yield return new TypeAttributeBinding(type, contracts.ToImmutable(), tags.ToImmutable(), lifetimes.FirstOrDefault());
    }

    private bool TagEquals(object? tag, object? otherTag) =>
        tag is ITypeSymbol tagType && otherTag is ITypeSymbol otherTagType
            ? typeSymbolComparer.RuntimeEquals(tagType, otherTagType)
            : Equals(tag, otherTag);

    private ITypeSymbol? GetTypeAttributeValue(AttributeData attributeData, int argumentPosition)
    {
        if (attributeData.AttributeClass is { IsGenericType: true, TypeArguments.Length: > 0 } attributeClass
            && argumentPosition < attributeClass.TypeArguments.Length)
        {
            return attributeClass.TypeArguments[argumentPosition];
        }

        if (argumentPosition < attributeData.ConstructorArguments.Length
            && attributeData.ConstructorArguments[argumentPosition].Value is ITypeSymbol type)
        {
            return type;
        }

        return null;
    }

    private static IEnumerable<object?> GetAttributeValues(AttributeData attributeData, int argumentPosition)
    {
        if (argumentPosition >= attributeData.ConstructorArguments.Length)
        {
            yield break;
        }

        var argument = attributeData.ConstructorArguments[argumentPosition];
        if (argument.Kind == TypedConstantKind.Array)
        {
            if (argument.Values.IsDefaultOrEmpty)
            {
                yield break;
            }

            foreach (var value in argument.Values.Select(i => i.Value))
            {
                yield return value;
            }

            yield break;
        }

        yield return argument.Value;
    }

    private static IEnumerable<object?> GetTagAttributeValues(AttributeData attributeData, int argumentPosition) =>
        GetAttributeValues(attributeData, argumentPosition)
            .Where(_ => !IsLifetimeAttributeArgument(attributeData, argumentPosition));

    private static bool IsLifetimeAttributeArgument(AttributeData attributeData, int argumentPosition)
    {
        if (argumentPosition >= attributeData.ConstructorArguments.Length)
        {
            return false;
        }

        var argument = attributeData.ConstructorArguments[argumentPosition];
        return IsLifetime(argument) || argument.Kind == TypedConstantKind.Array
            && !argument.Values.IsDefaultOrEmpty
            && IsLifetime(argument.Values[0]);
    }

    private static bool IsLifetime(TypedConstant argument) =>
        argument.Type is { TypeKind: TypeKind.Enum, Name: nameof(Lifetime) };

    private static Lifetime? GetLifetimeAttributeValue(AttributeData attributeData, int argumentPosition)
    {
        if (argumentPosition >= attributeData.ConstructorArguments.Length)
        {
            return null;
        }

        var argument = attributeData.ConstructorArguments[argumentPosition];
        if (argument.Kind == TypedConstantKind.Array)
        {
            if (argument.Values.IsDefaultOrEmpty)
            {
                return null;
            }

            argument = argument.Values[0];
        }

        return argument.Value switch
        {
            Lifetime lifetime => lifetime,
            int value => (Lifetime)value,
            _ => null
        };
    }

    private bool IsAttributeMatch(INamedTypeSymbol actualAttribute, INamedTypeSymbol registeredAttribute)
    {
        var actual = actualAttribute.IsGenericType
            ? actualAttribute.ConstructUnboundGenericType()
            : actualAttribute;

        var registered = registeredAttribute.IsGenericType
            ? registeredAttribute.ConstructUnboundGenericType()
            : registeredAttribute;

        return symbolNames.GetGlobalName(actual) == symbolNames.GetGlobalName(registered);
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    private static MdRoot ConfigureRoot(MdRoot root, IHints hints)
    {
        if (hints.IsLightweightAnonymousRoot)
        {
            if (root is { IsBuilder: false, RootType: INamedTypeSymbol { IsGenericType: false } }
                && string.IsNullOrWhiteSpace(root.Name))
            {
                root = root with { Kind = root.Kind | RootKinds.Light };
            }
        }

        return root;
    }
}
