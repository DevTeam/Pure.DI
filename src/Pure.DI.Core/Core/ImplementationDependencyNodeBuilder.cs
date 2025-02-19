// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class ImplementationDependencyNodeBuilder(
    IBuilder<DpImplementation, IEnumerable<DpImplementation>> implementationVariantsBuilder,
    IAttributes attributes,
    IInstanceDpProvider instanceDpProvider,
    Func<IConstructorInjectionsCounterWalker> constructorInjectionsCounterWalkerFactory)
    : IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(DependencyNodeBuildContext ctx)
    {
        var injectionsCounter = constructorInjectionsCounterWalkerFactory();
        var setup = ctx.Setup;
        foreach (var binding in setup.Bindings)
        {
            if (binding.Implementation is not {} implementation)
            {
                continue;
            }

            if (implementation.Type is not INamedTypeSymbol implementationType)
            {
                yield break;
            }

            if (implementationType.IsAbstract)
            {
                throw new CompileErrorException(
                    string.Format(Strings.Error_Template_CannotConstructAbstractType, implementationType),
                    implementation.Source.GetLocation(),
                    LogId.ErrorInvalidMetadata);
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
                        attributes.GetAttribute(setup.SemanticModel, setup.OrdinalAttributes, constructor, default(int?)),
                        instanceDpProvider.GetParameters(setup, constructor.Parameters, compilation, ctx.TypeConstructor)));
            }

            if (constructors.Count == 0)
            {
                throw new CompileErrorException(
                    string.Format(Strings.Error_Template_CannotBeInstantiatedNoAccessibleConstructor, implementationType),
                    implementation.Source.GetLocation(),
                    LogId.ErrorInvalidMetadata);
            }

            var instanceDp = instanceDpProvider.Get(setup, ctx.TypeConstructor, compilation, implementationType);
            var implementations = constructors
                .Select(constructor =>
                    new DpImplementation(
                        implementation,
                        binding,
                        constructor,
                        instanceDp.Methods,
                        instanceDp.Properties,
                        instanceDp.Fields))
                .ToList();

            var implementationsWithOrdinal = implementations
                .Where(i => i.Constructor.Ordinal.HasValue)
                .ToList();

            if (implementationsWithOrdinal.Count > 0)
            {
                foreach (var node in CreateNodes(injectionsCounter, implementationsWithOrdinal.OrderBy(i => i.Constructor.Ordinal)))
                {
                    yield return node;
                }

                continue;
            }

            foreach (var node in CreateNodes(injectionsCounter, implementations))
            {
                yield return node;
            }
        }
    }

    private IEnumerable<DependencyNode> CreateNodes(IConstructorInjectionsCounterWalker walker, IEnumerable<DpImplementation> implementations) =>
        implementations
            .OrderByDescending(i => GetInjectionsCount(walker, i.Constructor))
            .ThenByDescending(i => i.Constructor.Method.DeclaredAccessibility)
            .SelectMany(implementationVariantsBuilder.Build)
            .Select((implementation, variantId) => new DependencyNode(variantId, implementation.Binding, Implementation: implementation));

    private static int GetInjectionsCount(IConstructorInjectionsCounterWalker walker, in DpMethod constructor)
    {
        walker.VisitConstructor(Unit.Shared, constructor);
        return walker.Count;
    }
}