// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class RootsBuilder(
    IFastBuilder<ContractsBuildContext, ISet<Injection>> contractsBuilder,
    IBaseSymbolsProvider baseSymbolsProvider,
    ITypeSymbolComparer typeSymbolComparer)
    : IBuilder<DependencyGraph, DependencyGraph>
{
    public DependencyGraph Build(DependencyGraph dependencyGraph)
    {
        var rootsPairs = new List<KeyValuePair<Injection, Root>>();
        var compositionType = dependencyGraph.Source.SemanticModel.Compilation.GetTypeByMetadataName(dependencyGraph.Source.Name.FullName);
        foreach (var curNode in dependencyGraph.Graph.Vertices)
        {
            var node = curNode;
            if (node.Type is INamedTypeSymbol { IsUnboundGenericType: true })
            {
                continue;
            }

            if (node.Root is not {} root
                || !dependencyGraph.Graph.TryGetInEdges(node, out var rootDependencies)
                || rootDependencies.Count != 1)
            {
                continue;
            }

            if (!IsValidRoot(root.Source, compositionType))
            {
                continue;
            }

            var builderRoots = root.Source.BuilderRoots;
            if (!builderRoots.IsDefaultOrEmpty)
            {
                builderRoots= root.Source.BuilderRoots
                    .Where(i => IsValidRoot(i, compositionType))
                    .ToImmutableArray();

                if (builderRoots.IsDefaultOrEmpty)
                {
                    continue;
                }
            }

            node = rootDependencies.Single().Source;

            // ReSharper disable once LoopCanBeConvertedToQuery
            // ReSharper disable once UnusedVariable
            foreach (var injection in contractsBuilder.Build(new ContractsBuildContext(node.Binding, MdTag.ContextTag, root.Injection.Tag)).Where(i => IsRootContractMatch(i, root.Injection)).Take(1))
            {
                var rootInjection = ReferenceEquals(root.Injection.Tag, MdTag.ContextTag) ? root.Injection with { Tag = null } : root.Injection;
                rootsPairs.Add(new KeyValuePair<Injection, Root>(
                    rootInjection,
                    new Root(
                        0,
                        node,
                        root.Source with { BuilderRoots = builderRoots },
                        rootInjection,
                        root.Source.Name,
                        new Lines(),
                        ImmutableArray<VarDeclaration>.Empty,
                        root.Source.Kind,
                        default,
                        root.Source.IsBuilder)));
            }
        }

        var roots = rootsPairs
            .GroupBy(i => i.Key)
            .Select((byInjection, index) => (byInjection, index))
            .Select(group => CreateRoot(group))
            .ToImmutableArray();

        return dependencyGraph with { Roots = roots };
    }

    private static bool IsRootContractMatch(Injection contractInjection, Injection rootInjection)
    {
        if (contractInjection == rootInjection)
        {
            return true;
        }

        return rootInjection.Type is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated }
            && contractInjection == rootInjection with { Type = rootInjection.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) };
    }

    private bool IsValidRoot(MdRoot root, INamedTypeSymbol? compositionType)
    {
        var rootType = root.RootType;
        if (root.IsBuilder && typeSymbolComparer.RuntimeEquals(rootType, compositionType))
        {
            // Skip builders for the composition type
            return false;
        }

        // Check implemented
        return baseSymbolsProvider.GetBaseSymbols(rootType, (baseType, _) => typeSymbolComparer.RuntimeEquals(root.RootContractType, baseType)).Any();
    }

    private static Root CreateRoot((IEnumerable<KeyValuePair<Injection, Root>> byInjection, int index) group) =>
        group.byInjection
                .OrderByDescending(j => j.Value.IsPublic)
                .Select(j => j.Value)
                .First() with { Index = group.index + 1 };
}
