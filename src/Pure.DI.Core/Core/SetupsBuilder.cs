// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class SetupsBuilder(
    Func<IMetadataSyntaxWalker> metadataSyntaxWalkerFactory,
    ICache<ImmutableArray<byte>, bool> setupCache,
    Func<IBindingBuilder> bindingBuilderFactory,
    IArguments arguments,
    ITypeConstructor typeConstructor)
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

    public void VisitResolve(in MdResolver resolver)
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

    public void VisitHint(in MdHint hint) =>
        _hints[hint.Key] = hint.Value;

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

        return FinishSetup(default)!;
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
            where member.DeclaredAccessibility >= Accessibility.Internal && !member.IsStatic && member.CanBeReferencedByName && member is IFieldSymbol or IPropertySymbol or IMethodSymbol
            from attribute in member.GetAttributes()
            where attribute.AttributeClass?.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat) == Names.BindAttributeName
            select (attribute, member);

        foreach (var (attribute, member) in membersToBind)
        {
            var values = arguments.GetArgs(attribute.ConstructorArguments, attribute.NamedArguments, "type", "lifetime", "tags");
            ITypeSymbol? contractType = default;
            if (values.Length > 0 && values[0].Value is ITypeSymbol newContractType)
            {
                contractType = newContractType;
            }
            
            const string ctxName = "ctx_1182D127";
            const string valueName = "value";
            var instance = SyntaxFactory.IdentifierName(valueName);
            ExpressionSyntax value;

            var position = 0;
            var namespaces = new HashSet<string>();
            var resolvers = new List<MdResolver>();
            var block = new List<StatementSyntax>();
            switch (member)
            {
                case IFieldSymbol fieldSymbol:
                    contractType ??= fieldSymbol.Type;
                    value = SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        instance,
                        SyntaxFactory.IdentifierName(member.Name));
                    break;

                case IPropertySymbol propertySymbol:
                    contractType ??= propertySymbol.Type;
                    value = SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        instance,
                        SyntaxFactory.IdentifierName(member.Name));
                    break;

                case IMethodSymbol methodSymbol:
                    contractType ??= methodSymbol.ReturnType;

                    var args = methodSymbol.Parameters
                        .Select(i => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(i.Name)))
                        .ToArray();
                    
                    if (methodSymbol.IsGenericMethod)
                    {
                        typeConstructor.TryBind(setup, contractType, methodSymbol.ReturnType);
                        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                        foreach (var parameter in methodSymbol.Parameters)
                        {
                            var paramType = typeConstructor.ConstructReversed(setup, binding.SemanticModel.Compilation, parameter.Type);
                            block.Add(SyntaxFactory.ExpressionStatement(Inject(paramType, parameter.Name, resolvers, MdTag.ContextTag, ref position)));
                        }
                        
                        var typeArgs = new List<TypeSyntax>();
                        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                        foreach (var typeArg in methodSymbol.TypeArguments)
                        {
                            var argType = typeConstructor.ConstructReversed(setup, binding.SemanticModel.Compilation, typeArg);
                            var typeName = argType.ToString();
                            typeArgs.Add(SyntaxFactory.ParseTypeName(typeName));
                        }
                        
                        value = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            instance,
                            SyntaxFactory.GenericName(member.Name)
                                .AddTypeArgumentListArguments(typeArgs.ToArray()));
                    }
                    else
                    {
                        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                        foreach (var parameter in methodSymbol.Parameters)
                        {
                            block.Add(SyntaxFactory.ExpressionStatement(Inject(parameter.Type, parameter.Name, resolvers, MdTag.ContextTag, ref position)));
                        }
                        
                        value = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            instance,
                            SyntaxFactory.IdentifierName(member.Name));
                    }

                    value = SyntaxFactory
                        .InvocationExpression(value)
                        .AddArgumentListArguments(args);

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

            var contextParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(ctxName));
            object? valueTag = default;
            if (!contract.Tags.IsDefaultOrEmpty)
            {
                valueTag = contract.Tags.First().Value;
            }
            
            block.Add(SyntaxFactory.ExpressionStatement(Inject(contract.ContractType!, valueName, resolvers, valueTag, ref position)));
            block.Add(SyntaxFactory.ReturnStatement(value));
            var lambdaExpression = SyntaxFactory.SimpleLambdaExpression(contextParameter)
                .WithBlock(SyntaxFactory.Block(block));

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
                VisitTag(new MdTag(tagPosition, default));
            }

            VisitFactory(
                new MdFactory(
                    semanticModel,
                    source,
                    contractType,
                    lambdaExpression,
                    contextParameter,
                    resolvers.ToImmutableArray(),
                    false));
            
            VisitUsingDirectives(new MdUsingDirectives(namespaces.ToImmutableArray(), ImmutableArray<string>.Empty));
            continue;

            InvocationExpressionSyntax Inject(ITypeSymbol injectedType, string injectedName, ICollection<MdResolver> resolversSet, object? tag, ref int curPosition)
            {
                namespaces.Add(injectedType.ContainingNamespace.ToString());
                resolversSet.Add(new MdResolver
                {
                    SemanticModel = semanticModel,
                    Source = source,
                    ContractType = injectedType,
                    Tag = new MdTag(curPosition, tag),
                    Position = curPosition
                });

                curPosition++;
                
                var valueDeclaration = SyntaxFactory.DeclarationExpression(
                    SyntaxFactory.ParseTypeName(injectedType.ToString()).WithTrailingTrivia(SyntaxFactory.Space),
                    SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(injectedName)));

                var valueArg =
                    SyntaxFactory.Argument(valueDeclaration)
                        .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword));

                return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(ctxName),
                            SyntaxFactory.IdentifierName(nameof(IContext.Inject))))
                    .AddArgumentListArguments(valueArg);
            }
        }
    }

    private MdSetup? FinishSetup(MdSetup? source)
    {
        if (_setup is not { } setup)
        {
            return default;
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
        _setup = default;
        _bindingBuilder = bindingBuilderFactory();
        return setup;
    }
}