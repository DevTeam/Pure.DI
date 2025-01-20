// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal sealed class SetupsBuilder(
    Func<IMetadataSyntaxWalker> metadataSyntaxWalkerFactory,
    ICache<ImmutableArray<byte>, bool> setupCache,
    Func<IBindingBuilder> bindingBuilderFactory,
    IArguments arguments,
    Func<ITypeConstructor> typeConstructorFactory,
    ISemantic semantic,
    ISymbolNames symbolNames)
    : IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>, IMetadataVisitor, ISetupFinalizer
{
    private readonly List<MdSetup> _setups = [];
    private readonly List<MdBinding> _bindings = [];
    private readonly List<MdRoot> _roots = [];
    private readonly List<MdDependsOn> _dependsOn = [];
    private readonly List<MdGenericTypeArgument> _genericTypeArguments = [];
    private readonly List<MdGenericTypeArgumentAttribute> _genericTypeArgumentAttributes = [];
    private readonly List<MdTypeAttribute> _typeAttributes = [];
    private readonly List<MdTagAttribute> _tagAttributes = [];
    private readonly List<MdOrdinalAttribute> _ordinalAttributes = [];
    private readonly List<MdUsingDirectives> _usingDirectives = [];
    private readonly List<MdAccumulator> _accumulators = [];
    private IBindingBuilder _bindingBuilder = bindingBuilderFactory();
    private MdSetup? _setup;
    private Hints _hints = new();

    public IEnumerable<MdSetup> Build(SyntaxUpdate update)
    {
        var checkSum = update.Node.SyntaxTree.GetText().GetChecksum();
        if (!setupCache.Get(checkSum, _ => true))
        {
            return Array.Empty<MdSetup>();
        }

        metadataSyntaxWalkerFactory().Visit(this, update);
        if (!_setups.Any())
        {
            setupCache.Set(checkSum, false);
        }

        return _setups;
    }

    public void VisitSetup(in MdSetup setup)
    {
        FinishSetup(_setup);
        _setup = setup;
    }

    public void VisitUsingDirectives(in MdUsingDirectives usingDirectives)
    {
        if (usingDirectives.UsingDirectives.Length == 0 && usingDirectives.StaticUsingDirectives.Length == 0)
        {
            return;
        }

        _usingDirectives.Add(usingDirectives);
    }

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

    public void VisitDefaultLifetime(in MdDefaultLifetime defaultLifetime) =>
        _bindingBuilder.AddDefaultLifetime(defaultLifetime);

    public void VisitDependsOn(in MdDependsOn dependsOn) =>
        _dependsOn.Add(dependsOn);

    public void VisitTypeAttribute(in MdTypeAttribute typeAttribute) =>
        _typeAttributes.Add(typeAttribute);

    public void VisitTagAttribute(in MdTagAttribute tagAttribute) =>
        _tagAttributes.Add(tagAttribute);

    public void VisitOrdinalAttribute(in MdOrdinalAttribute ordinalAttribute) =>
        _ordinalAttributes.Add(ordinalAttribute);

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

    private void FinishBinding() =>
        _bindings.Add(_bindingBuilder.Build(_setup!));

    public MdSetup Finalize(MdSetup setup)
    {
        _setup = setup;
        _hints = new Hints();
        foreach (var item in setup.Hints)
        {
            _hints[item.Key] = item.Value;
        }

        _bindings.AddRange(setup.Bindings);
        _roots.AddRange(setup.Roots);
        _dependsOn.AddRange(setup.DependsOn);
        _genericTypeArguments.AddRange(setup.GenericTypeArguments);
        _genericTypeArgumentAttributes.AddRange(setup.GenericTypeArgumentAttributes);
        _typeAttributes.AddRange(setup.TypeAttributes);
        _tagAttributes.AddRange(setup.TagAttributes);
        _ordinalAttributes.AddRange(setup.OrdinalAttributes);
        _usingDirectives.AddRange(setup.UsingDirectives);
        _accumulators.AddRange(setup.Accumulators);
        foreach (var binding in setup.Bindings)
        {
            FinalizeBinding(setup, binding);
        }

        return FinishSetup(null)!;
    }

    private void FinalizeBinding(MdSetup setup, MdBinding binding)
    {
        ITypeSymbol type;
        if (binding.Arg is { } arg)
        {
            type = arg.Type;
        }
        else
        {
            if (binding.Implementation is { } implementation)
            {
                type = implementation.Type;
            }
            else
            {
                if (binding.Factory is { } factory)
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

        var membersToBind =
            from member in type.GetMembers()
            where member.DeclaredAccessibility >= Accessibility.Internal && member.CanBeReferencedByName && member is IFieldSymbol or IPropertySymbol or IMethodSymbol
            from attribute in member.GetAttributes()
            where attribute.AttributeClass is not null && symbolNames.GetGlobalName(attribute.AttributeClass) == Names.BindAttributeName
            select (attribute, member);

        var typeConstructor = typeConstructorFactory();
        foreach (var (attribute, member) in membersToBind)
        {
            var values = arguments.GetArgs(attribute.ConstructorArguments, attribute.NamedArguments, "type", "lifetime", "tags");
            ITypeSymbol? contractType = null;
            if (values.Length > 0 && values[0].Value is ITypeSymbol newContractType)
            {
                contractType = newContractType;
            }

            var position = 0;
            var namespaces = new HashSet<string>();
            var resolvers = new List<MdResolver>();
            switch (member)
            {
                case IFieldSymbol fieldSymbol:
                    contractType ??= fieldSymbol.Type;
                    break;

                case IPropertySymbol propertySymbol:
                    contractType ??= propertySymbol.Type;
                    break;

                case IMethodSymbol methodSymbol:
                    contractType ??= methodSymbol.ReturnType;
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

            object? valueTag = null;
            if (!contract.Tags.IsDefaultOrEmpty)
            {
                valueTag = contract.Tags.First().Value;
            }

            if (!member.IsStatic)
            {
                resolvers.Add(CreateResolver(typeConstructor, FactoryCodeBuilder.DefaultInstanceValueName, contract.ContractType!, valueTag, ref position));
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

            var memberResolver = CreateResolver(typeConstructor, FactoryCodeBuilder.DefaultInstanceValueName, contract.ContractType!, valueTag, ref position);
            memberResolver = memberResolver with { Member = member };
            VisitFactory(
                new MdFactory(
                    semanticModel,
                    source,
                    contractType,
                    FactoryCodeBuilder.DefaultBindAttrParenthesizedLambda,
                    true,
                    FactoryCodeBuilder.DefaultCtxParameter,
                    resolvers.ToImmutableArray(),
                    ImmutableArray<MdInitializer>.Empty,
                    false,
                    memberResolver));

            VisitUsingDirectives(new MdUsingDirectives(namespaces.ToImmutableArray(), ImmutableArray<string>.Empty));
            continue;

            MdResolver CreateResolver(ITypeConstructor constructor, string name, ITypeSymbol injectedType, object? tag, ref int curPosition)
            {
                var typeSyntax = SyntaxFactory.ParseTypeName(symbolNames.GetGlobalName(injectedType));
                if (semantic.IsValidNamespace(injectedType.ContainingNamespace))
                {
                    namespaces.Add(injectedType.ContainingNamespace.ToString());
                }

                return new MdResolver
                {
                    SemanticModel = semanticModel,
                    Source = source,
                    ContractType = injectedType,
                    Tag = new MdTag(curPosition, tag),
                    ArgumentType = typeSyntax,
                    Parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)).WithType(typeSyntax),
                    Position = curPosition++,
                    TypeConstructor = constructor
                };
            }
        }
    }

    private MdSetup? FinishSetup(MdSetup? source)
    {
        if (_setup is not { } setup)
        {
            return null;
        }

        setup = setup with
        {
            Hints = _hints,
            Roots = _roots.ToImmutableArray(),
            Bindings = _bindings.Select(i => i with { SourceSetup = source ?? i.SourceSetup }).ToImmutableArray(),
            DependsOn = _dependsOn.ToImmutableArray(),
            GenericTypeArguments = _genericTypeArguments.ToImmutableArray(),
            GenericTypeArgumentAttributes = _genericTypeArgumentAttributes.ToImmutableArray(),
            TypeAttributes = _typeAttributes.ToImmutableArray(),
            TagAttributes = _tagAttributes.ToImmutableArray(),
            OrdinalAttributes = _ordinalAttributes.ToImmutableArray(),
            UsingDirectives = _usingDirectives.ToImmutableArray(),
            Accumulators = _accumulators.Distinct().ToImmutableArray()
        };

        _setups.Add(setup);
        _hints = new Hints();
        _bindings.Clear();
        _roots.Clear();
        _dependsOn.Clear();
        _genericTypeArguments.Clear();
        _genericTypeArgumentAttributes.Clear();
        _typeAttributes.Clear();
        _ordinalAttributes.Clear();
        _usingDirectives.Clear();
        _accumulators.Clear();
        _setup = null;
        _bindingBuilder = bindingBuilderFactory();
        return setup;
    }
}