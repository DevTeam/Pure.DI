// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable LoopCanBeConvertedToQuery

namespace Pure.DI.Core;

sealed class MetadataBuilder(
    Func<IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>> setupsBuilderFactory,
    Func<ISetupFinalizer> setupFinalizerFactory,
    Func<SetupContextRewriterContext, ISetupContextRewriter> setupContextRewriterFactory,
    Func<SetupContextMembersCollectorContext, ISetupContextMembersCollector> setupContextMembersCollectorFactory,
    ICompilations compilations,
    IRegistryManager<int> bindingsRegistryManager,
    ILocationProvider locationProvider,
    CancellationToken cancellationToken)
    : IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>>
{
    private readonly record struct SetupDependency(
        MdSetup Setup,
        string? ContextArgName,
        ExpressionSyntax? ContextArgSource,
        SetupContextKind ContextArgKind,
        MdDependsOn? DependsOn);

    public IEnumerable<MdSetup> Build(IEnumerable<SyntaxUpdate> updates)
    {
        var actualUpdates = new List<SyntaxUpdate>();
        var visitedTrees = new HashSet<SyntaxTree>();
        foreach (var update in updates)
        {
            var tree = update.Node.SyntaxTree;
            if (visitedTrees.Add(tree))
            {
                actualUpdates.Add(update);
            }
        }

        var setups = new List<MdSetup>();
        foreach (var update in actualUpdates)
        {
            var languageVersion = compilations.GetLanguageVersion(update.SemanticModel.Compilation);
            if (languageVersion < LanguageVersion.CSharp8)
            {
                throw new CompileErrorException(
                    string.Format(Strings.Error_Template_UnsupportLanguage, Names.GeneratorName, languageVersion.ToDisplayString(), LanguageVersion.CSharp8.ToDisplayString()),
                    ImmutableArray.Create(locationProvider.GetLocation(update.Node)),
                    LogId.ErrorNotSupportedLanguageVersion,
                    nameof(Strings.Error_Template_UnsupportLanguage));
            }

            var setupsBuilder = setupsBuilderFactory();
            foreach (var newSetup in setupsBuilder.Build(update))
            {
                setups.Add(newSetup);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        if (setups.Count == 0)
        {
            yield break;
        }

        var setupMap = setups
            .Where(i => i.Kind != CompositionKind.Global)
            .GroupBy(i => i.Name)
            .Select(setupGroup => {
                MergeSetups(setupGroup.Select(i => new SetupDependency(i, null, null, SetupContextKind.Argument, null)), out var mergedSetup, false);
                return mergedSetup;
            })
            .ToDictionary(i => i.Name, i => i);

        var globalSetups = setups.Where(i => i.Kind == CompositionKind.Global).OrderBy(i => i.Name.ClassName).ToList();
        foreach (var setup in setupMap.Values.Where(i => i.Kind == CompositionKind.Public).OrderBy(i => i.Name))
        {
            var setupsChain = globalSetups
                .Select(i => new SetupDependency(i, null, null, SetupContextKind.Argument, null))
                .Concat(ResolveDependencies(setup, setupMap, new HashSet<CompositionName>()))
                .Concat(Enumerable.Repeat(new SetupDependency(setup, null, null, SetupContextKind.Argument, null), 1));

            MergeSetups(setupsChain, out var mergedSetup, true);
            var setupFinalizer = setupFinalizerFactory();
            yield return setupFinalizer.Finalize(mergedSetup, setupMap);
        }
    }

    private IEnumerable<SetupDependency> ResolveDependencies(
        MdSetup setup,
        IReadOnlyDictionary<CompositionName, MdSetup> map,
        ISet<CompositionName> processed)
    {
        foreach (var dependsOn in setup.DependsOn.OrderByDescending(i => i.Explicit))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var item in dependsOn.Items)
            {
                if (!processed.Add(item.CompositionTypeName))
                {
                    continue;
                }

                if (!map.TryGetValue(item.CompositionTypeName, out var dependsOnSetup))
                {
                    if (!dependsOn.Explicit)
                    {
                        continue;
                    }

                    throw new CompileErrorException(
                        string.Format(Strings.Error_Template_CannotFindSetup, item.CompositionTypeName),
                        ImmutableArray.Create(locationProvider.GetLocation(dependsOn.Source)),
                        LogId.ErrorCannotFindSetup,
                        nameof(Strings.Error_Template_CannotFindSetup));
                }

                if (!dependsOn.Explicit && dependsOnSetup.Kind != CompositionKind.Internal)
                {
                    continue;
                }

                yield return new SetupDependency(dependsOnSetup, item.ContextArgName, item.ContextArgSource, item.ContextArgKind, dependsOn);
                foreach (var result in ResolveDependencies(dependsOnSetup, map, processed))
                {
                    yield return result;
                }
            }
        }
    }

    private void MergeSetups(IEnumerable<SetupDependency> setups, out MdSetup mergedSetup, bool resolveDependsOn)
    {
        MdSetup? lastSetup = null;
        var contextArgs = new List<(string ArgName, ExpressionSyntax? ArgSource, ITypeSymbol ArgType, SetupContextKind Kind, MdDependsOn DependsOn, SemanticModel SemanticModel)>();
        var name = new CompositionName("Composition", "", null);
        var kind = CompositionKind.Global;
        var settings = new Hints();
        var bindingsBuilder = ImmutableArray.CreateBuilder<MdBinding>(64);
        var rootsBuilder = ImmutableArray.CreateBuilder<MdRoot>(64);
        var dependsOnBuilder = ImmutableArray.CreateBuilder<MdDependsOn>(2);
        var genericTypeArgumentBuilder = ImmutableArray.CreateBuilder<MdGenericTypeArgument>(0);
        var genericTypeArgumentAttributesBuilder = ImmutableArray.CreateBuilder<MdGenericTypeArgumentAttribute>(1);
        var typeAttributesBuilder = ImmutableArray.CreateBuilder<MdTypeAttribute>(2);
        var tagAttributesBuilder = ImmutableArray.CreateBuilder<MdTagAttribute>(2);
        var specialTypeBuilder = ImmutableArray.CreateBuilder<MdSpecialType>(0);
        var ordinalAttributesBuilder = ImmutableArray.CreateBuilder<MdOrdinalAttribute>(2);
        var usingDirectives = ImmutableArray.CreateBuilder<MdUsingDirectives>(2);
        var accumulators = ImmutableArray.CreateBuilder<MdAccumulator>(1);
        var setupContextMembers = new List<SetupContextMembers>();
        var defaultLifetimes = new List<MdDefaultLifetime>();
        var bindingId = 0;
        var comments = new List<string>();
        // ReSharper disable once UseDeconstruction
        foreach (var item in setups)
        {
            var setup = item.Setup;
            lastSetup = setup;
            name = setup.Name;
            kind = setup.Kind;
            foreach (var setting in setup.Hints)
            {
                var items = settings.GetOrAdd(setting.Key, _ => new LinkedList<string>());
                foreach (var newValue in setting.Value)
                {
                    items.AddLast(newValue);
                }
            }

            if (resolveDependsOn)
            {
                foreach (var binding in setup.Bindings)
                {
                    var updated = binding;

                    // ReSharper disable once InvertIf
                    if (item.ContextArgName is { Length: > 0 }
                        && binding.Factory is { } factory
                        && item.ContextArgKind != SetupContextKind.Members
                        && GetContainingType(setup) is { } containingSetupType)
                    {
                        var rewriterContext = new SetupContextRewriterContext(
                            factory.SemanticModel,
                            containingSetupType,
                            item.ContextArgName,
                            item.ContextArgKind,
                            factory.IsSimpleFactory,
                            factory.Context);
                        var rewritten = setupContextRewriterFactory(rewriterContext).Rewrite(factory.Factory);
                        var updatedFactory = factory with { Factory = rewritten };
                        if (item.ContextArgKind == SetupContextKind.RootArgument)
                        {
                            updatedFactory = updatedFactory with
                            {
                                Resolvers = AddRootArgumentResolver(updatedFactory, rewritten, containingSetupType, item.ContextArgName)
                            };
                        }

                        updated = updated with { Factory = updatedFactory };
                    }

                    bindingsBuilder.Add(updated with { Id = bindingId++ });
                }
            }
            else
            {
                bindingsBuilder.AddRange(setup.Bindings);
            }

            rootsBuilder.AddRange(setup.Roots);
            dependsOnBuilder.AddRange(setup.DependsOn);
            genericTypeArgumentBuilder.AddRange(setup.GenericTypeArguments);
            genericTypeArgumentAttributesBuilder.AddRange(setup.GenericTypeArgumentAttributes);
            typeAttributesBuilder.AddRange(setup.TypeAttributes);
            tagAttributesBuilder.AddRange(setup.TagAttributes);
            ordinalAttributesBuilder.AddRange(setup.OrdinalAttributes);
            specialTypeBuilder.AddRange(setup.SpecialTypes);
            accumulators.AddRange(setup.Accumulators);
            defaultLifetimes.AddRange(setup.DefaultLifetimes);
            if (!setup.SetupContextMembers.IsDefaultOrEmpty)
            {
                setupContextMembers.AddRange(setup.SetupContextMembers);
            }

            foreach (var usingDirective in setup.UsingDirectives)
            {
                usingDirectives.Add(usingDirective);
            }

            if (setup.Kind == CompositionKind.Public)
            {
                comments.AddRange(setup.Comments);
            }

            if (resolveDependsOn
                && item is { ContextArgName.Length: > 0, DependsOn: { } dependsOn }
                && GetContainingType(setup) is { } setupType)
            {
                if (item.ContextArgKind == SetupContextKind.Members)
                {
                    var targetType = GetContainingType(dependsOn) ?? setupType;
                    var collectorContext = new SetupContextMembersCollectorContext(setup, setupType, targetType);
                    var members = setupContextMembersCollectorFactory(collectorContext).Collect();
                    setupContextMembers.Add(new SetupContextMembers(setup.Name, item.ContextArgName, members));
                }
                else
                {
                    contextArgs.Add((item.ContextArgName, item.ContextArgSource, setupType, item.ContextArgKind, dependsOn, dependsOn.SemanticModel));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        if (resolveDependsOn && lastSetup is not null && contextArgs.Count > 0)
        {
            foreach (var (argName, argSource, argType, contextKind, dependsOn, semanticModel) in contextArgs)
            {
                var argLocation = argSource ?? dependsOn.Source;
                var argKind = contextKind == SetupContextKind.RootArgument ? ArgKind.Root : ArgKind.Composition;
                var arg = new MdArg(semanticModel, argLocation, argType, argName, argKind, false, [], true, contextKind);
                var binding = new MdBinding(
                    bindingId++,
                    argLocation,
                    lastSetup,
                    semanticModel,
                    ImmutableArray.Create(new MdContract(semanticModel, argLocation, argType, ContractKind.Explicit, ImmutableArray<MdTag>.Empty)),
                    ImmutableArray<MdTag>.Empty,
                    null,
                    null,
                    null,
                    arg);
                bindingsBuilder.Add(binding);
                bindingsRegistryManager.Register(lastSetup, binding.Id);
            }
        }

        var bindings = bindingsBuilder.ToImmutable();

        var tagOn = BuildTagOn(bindings);

        var mergedSetupContextMembers = setupContextMembers
            .GroupBy(member => member.SetupName)
            .Select(group => new SetupContextMembers(
                group.Key,
                group.Select(i => i.ContextName).FirstOrDefault() ?? "",
                group.SelectMany(i => i.Members)
                    .GroupBy(i => (i.SyntaxTree, i.Span))
                    .Select(i => i.First())
                    .ToImmutableArray()))
            .ToImmutableArray();

        mergedSetup = new MdSetup(
            lastSetup?.SemanticModel!,
            lastSetup?.Source!,
            name,
            usingDirectives.ToImmutableArray(),
            kind,
            settings,
            bindings,
            rootsBuilder.ToImmutable(),
            resolveDependsOn ? ImmutableArray<MdDependsOn>.Empty : dependsOnBuilder.ToImmutable(),
            genericTypeArgumentBuilder.ToImmutableArray(),
            genericTypeArgumentAttributesBuilder.ToImmutableArray(),
            typeAttributesBuilder.ToImmutable(),
            tagAttributesBuilder.ToImmutable(),
            ordinalAttributesBuilder.ToImmutable(),
            specialTypeBuilder.ToImmutable(),
            accumulators.ToImmutable(),
            mergedSetupContextMembers,
            tagOn,
            comments,
            defaultLifetimes.ToImmutableArray());

        return;

        static List<MdTagOnSites> BuildTagOn(ImmutableArray<MdBinding> bindings)
        {
            if (bindings.IsDefaultOrEmpty)
            {
                return [];
            }

            IReadOnlyList<MdBinding> orderedBindings = bindings;
            if (!IsSortedById(bindings))
            {
                var copy = bindings.ToArray();
                Array.Sort(copy, static (x, y) => x.Id.CompareTo(y.Id));
                orderedBindings = copy;
            }

            var result = new List<MdTagOnSites>();
            var seen = new HashSet<MdTagOnSites>();
            for (var i = 0; i < orderedBindings.Count; i++)
            {
                var binding = orderedBindings[i];
                foreach (var contract in binding.Contracts)
                {
                    foreach (var tag in contract.Tags)
                    {
                        if (tag.Value is MdTagOnSites tagOnSites
                            && tagOnSites.InjectionSites.Length > 0
                            && seen.Add(tagOnSites))
                        {
                            result.Add(tagOnSites);
                        }
                    }
                }
            }

            result.Reverse();
            return result;
        }

        static bool IsSortedById(ImmutableArray<MdBinding> bindings)
        {
            var last = int.MinValue;
            foreach (var binding in bindings)
            {
                var id = binding.Id;
                if (id < last)
                {
                    return false;
                }

                last = id;
            }

            return true;
        }
    }

    private static ImmutableArray<MdResolver> AddRootArgumentResolver(
        MdFactory factory,
        LambdaExpressionSyntax rewritten,
        INamedTypeSymbol setupType,
        string contextArgName)
    {
        if (factory.Resolvers.Any(resolver =>
                SymbolEqualityComparer.Default.Equals(resolver.ContractType, setupType)
                && resolver.Parameter is { Identifier.Text: var name }
                && name == contextArgName))
        {
            return factory.Resolvers;
        }

        ParameterSyntax? parameter = null;
        var position = factory.Resolvers.Length;
        switch (rewritten)
        {
            case ParenthesizedLambdaExpressionSyntax parenthesized:
                for (var index = 0; index < parenthesized.ParameterList.Parameters.Count; index++)
                {
                    var current = parenthesized.ParameterList.Parameters[index];
                    // ReSharper disable once InvertIf
                    if (current.Identifier.Text == contextArgName)
                    {
                        parameter = current;
                        position = index;
                        break;
                    }
                }

                break;

            case SimpleLambdaExpressionSyntax simple
                when simple.Parameter.Identifier.Text == contextArgName:
                parameter = simple.Parameter;
                position = 0;
                break;
        }

        var contextParameter = parameter ?? SyntaxFactory.Parameter(SyntaxFactory.Identifier(contextArgName))
            .WithType(SyntaxFactory.ParseTypeName(setupType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        var argumentType = contextParameter.Type ?? SyntaxFactory.ParseTypeName(setupType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        var attributes = contextParameter.AttributeLists.SelectMany(list => list.Attributes).ToImmutableArray();

        var resolver = new MdResolver
        {
            SemanticModel = factory.SemanticModel,
            Source = factory.Source,
            ContractType = setupType,
            Tag = new MdTag(0, null),
            ArgumentType = argumentType,
            Parameter = contextParameter,
            Position = position,
            Attributes = attributes
        };

        return factory.Resolvers.Add(resolver);
    }

    private static INamedTypeSymbol? GetContainingType(MdSetup setup) =>
        setup.Source.Ancestors()
            .OfType<BaseTypeDeclarationSyntax>()
            .FirstOrDefault() is { } typeDeclaration
            ? setup.SemanticModel.GetDeclaredSymbol(typeDeclaration)
            : null;

    private static INamedTypeSymbol? GetContainingType(MdDependsOn dependsOn) =>
        dependsOn.Source.Ancestors()
            .OfType<BaseTypeDeclarationSyntax>()
            .FirstOrDefault() is { } typeDeclaration
            ? dependsOn.SemanticModel.GetDeclaredSymbol(typeDeclaration)
            : null;
}
