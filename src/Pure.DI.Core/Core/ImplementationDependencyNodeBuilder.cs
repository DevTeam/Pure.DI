// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal sealed class ImplementationDependencyNodeBuilder(
    IBuilder<DpImplementation, IEnumerable<DpImplementation>> implementationVariantsBuilder,
    IAttributes attributes,
    IInstanceDpProvider instanceDpProvider)
    : IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(MdSetup setup)
    {
        var injectionsWalker = new DependenciesToInjectionsCountWalker();
        foreach (var binding in setup.Bindings)
        {
            if (binding.Implementation is not { } implementation)
            {
                continue;
            }

            if (implementation.Type is not INamedTypeSymbol implementationType)
            {
                yield break;
            }

            if (implementationType.IsAbstract)
            {
                throw new CompileErrorException($"The {implementationType} cannot be constructed due to being an abstract type.", implementation.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            }

            var compilation = binding.SemanticModel.Compilation;
            var constructors = new List<DpMethod>();
            var hasExplicitlyDeclaredNonStaticCtor = implementationType.Constructors.Any(i => !i.IsImplicitlyDeclared && !i.IsStatic);
            foreach (var constructor in implementationType.Constructors)
            {
                if (hasExplicitlyDeclaredNonStaticCtor && constructor.IsImplicitlyDeclared)
                {
                    continue;
                }

                if (constructor.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.Public))
                {
                    continue;
                }

                constructors.Add(
                    new DpMethod(
                        constructor,
                        attributes.GetAttribute(setup.OrdinalAttributes, constructor, default(int?)),
                        instanceDpProvider.GetParameters(setup, constructor.Parameters, compilation, setup.TypeConstructor)));
            }

            if (!constructors.Any())
            {
                throw new CompileErrorException($"The instance of {implementationType} cannot be instantiated due to no accessible constructor available.", implementation.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            }

            var instanceDp = instanceDpProvider.Get(setup, compilation, implementationType);
            var implementations = constructors
                .Select(constructor =>
                    new DpImplementation(
                        implementation,
                        binding,
                        constructor,
                        instanceDp.Methods,
                        instanceDp.Properties,
                        instanceDp.Fields))
                .ToArray();

            var implementationsWithOrdinal = implementations
                .Where(i => i.Constructor.Ordinal.HasValue)
                .ToArray();

            if (implementationsWithOrdinal.Any())
            {
                foreach (var node in CreateNodes(injectionsWalker, implementationsWithOrdinal.OrderBy(i => i.Constructor.Ordinal)))
                {
                    yield return node;
                }

                continue;
            }

            foreach (var node in CreateNodes(injectionsWalker, implementations))
            {
                yield return node;
            }
        }
    }

    private IEnumerable<DependencyNode> CreateNodes(DependenciesToInjectionsCountWalker walker, IEnumerable<DpImplementation> implementations) =>
        implementations
            .OrderByDescending(i => GetInjectionsCount(walker, i.Constructor))
            .ThenByDescending(i => i.Constructor.Method.DeclaredAccessibility)
            .SelectMany(implementationVariantsBuilder.Build)
            .Select((implementation, variantId) => new DependencyNode(variantId, implementation.Binding, Implementation: implementation));

    private static int GetInjectionsCount(DependenciesToInjectionsCountWalker walker, in DpMethod constructor)
    {
        walker.VisitConstructor(Unit.Shared, constructor);
        return walker.Count;
    }
    
    private sealed class DependenciesToInjectionsCountWalker : DependenciesWalker<Unit>
    {
        public int Count { get; private set; }

        public override void VisitConstructor(in Unit ctx, in DpMethod constructor)
        {
            Count = 0;
            base.VisitConstructor(in ctx, in constructor);
        }

        public override void VisitInjection(
            in Unit ctx,
            in Injection injection,
            bool hasExplicitDefaultValue,
            object? explicitDefaultValue,
            in ImmutableArray<Location> locations) => Count++;
    }
}