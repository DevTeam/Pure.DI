// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core;

internal sealed class MetadataBuilder(
    Func<IBuilder<SyntaxUpdate, IEnumerable<MdSetup>>> setupsBuilderFactory,
    CancellationToken cancellationToken)
    : IBuilder<IEnumerable<SyntaxUpdate>, IEnumerable<MdSetup>>
{
    public IEnumerable<MdSetup> Build(IEnumerable<SyntaxUpdate> updates)
    {
        var actualUpdates = 
            updates
                .GroupBy(i => i.Node.SyntaxTree.GetRoot())
                .Select(i => new SyntaxUpdate(i.Key, i.First().SemanticModel))
                .ToList();
        
        var setups = new List<MdSetup>();
        foreach (var update in actualUpdates)
        {
            var languageVersion = update.SemanticModel.Compilation.GetLanguageVersion();
            if (languageVersion < LanguageVersion.CSharp8)
            {
                throw new CompileErrorException($"{Names.GeneratorName} does not support C# {languageVersion.ToDisplayString()}. Please use language version {LanguageVersion.CSharp8.ToDisplayString()} or greater.", update.Node.GetLocation(), LogId.ErrorNotSupportedLanguageVersion);
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
            .Select(setupGroup =>
            {
                MergeSetups(setupGroup, out var mergedSetup, false);
                return mergedSetup;
            })
            .ToDictionary(i =>  i.Name, i => i);
        
        var globalSetups = setups.Where(i => i.Kind == CompositionKind.Global).OrderBy(i => i.Name.ClassName).ToList();
        foreach (var setup in setupMap.Values.Where(i => i.Kind == CompositionKind.Public).OrderBy(i => i.Name))
        {
            var setupsChain = globalSetups
                .Concat(ResolveDependencies(setup, setupMap, new HashSet<CompositionName>()))
                .Concat(Enumerable.Repeat(setup, 1));
            
            MergeSetups(setupsChain, out var mergedSetup, true);
            yield return mergedSetup;
        }
    }

    private IEnumerable<MdSetup> ResolveDependencies(
        MdSetup setup,
        IReadOnlyDictionary<CompositionName, MdSetup> map, ISet<CompositionName> processed)
    {
        foreach (var dependsOn in setup.DependsOn)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var compositionTypeName in dependsOn.CompositionTypeNames)
            {
                if (!processed.Add(compositionTypeName))
                {
                    continue;
                }

                if (!map.TryGetValue(compositionTypeName, out var dependsOnSetup))
                {
                    throw new CompileErrorException($"Cannot find setup \"{compositionTypeName}\".", dependsOn.Source.GetLocation(), LogId.ErrorCannotFindSetup);
                }

                yield return dependsOnSetup;
                foreach (var result in ResolveDependencies(dependsOnSetup, map, processed))
                {
                    yield return result;
                }   
            }
        }
    }

    private void MergeSetups(IEnumerable<MdSetup> setups, out MdSetup mergedSetup, bool resolveDependsOn)
    {
        MdSetup? lastSetup = default;
        var name = new CompositionName("Composition", "", default);
        var kind = CompositionKind.Global;
        var settings = new Hints();
        var bindingsBuilder = ImmutableArray.CreateBuilder<MdBinding>(64);
        var rootsBuilder = ImmutableArray.CreateBuilder<MdRoot>(64);
        var dependsOnBuilder = ImmutableArray.CreateBuilder<MdDependsOn>(2);
        var genericTypeArgumentBuilder = ImmutableArray.CreateBuilder<MdGenericTypeArgument>(0);
        var genericTypeArgumentAttributesBuilder = ImmutableArray.CreateBuilder<MdGenericTypeArgumentAttribute>(1);
        var typeAttributesBuilder = ImmutableArray.CreateBuilder<MdTypeAttribute>(2);
        var tagAttributesBuilder = ImmutableArray.CreateBuilder<MdTagAttribute>(2);
        var ordinalAttributesBuilder = ImmutableArray.CreateBuilder<MdOrdinalAttribute>(2);
        var usingDirectives = ImmutableArray.CreateBuilder<MdUsingDirectives>(2);
        var accumulators = ImmutableArray.CreateBuilder<MdAccumulator>(1);
        var bindingId = 0;
        var comments = new List<string>();
        foreach (var setup in setups)
        {
            lastSetup = setup;
            name = setup.Name;
            kind = setup.Kind;
            foreach (var setting in setup.Hints)
            {
                settings[setting.Key] = setting.Value;
            }

            if (resolveDependsOn)
            {
                bindingsBuilder.AddRange(setup.Bindings.Select(i => i with { Id = bindingId++ }));
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
            accumulators.AddRange(setup.Accumulators);
            foreach (var usingDirective in setup.UsingDirectives)
            {
                usingDirectives.Add(usingDirective);   
            }

            if (setup.Kind == CompositionKind.Public)
            {
                comments.AddRange(setup.Comments);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        var bindings = bindingsBuilder.ToImmutable();
        
        var tagOn = bindings
            .OrderBy(i => i.Id)
            .SelectMany(i => i.Contracts)
            .SelectMany(binding => binding.Tags.Select(i => i.Value).OfType<MdTagOnSites>())
            .Where(i => i.InjectionSites.Length > 0)
            .Distinct()
            .Reverse()
            .ToList();

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
            accumulators.ToImmutable(),
            tagOn,
            comments);
    }
}