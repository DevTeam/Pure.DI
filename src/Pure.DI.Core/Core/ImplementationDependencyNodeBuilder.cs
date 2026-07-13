// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class ImplementationDependencyNodeBuilder(
    IBuilder<DpImplementation, IEnumerable<DpImplementation>> implementationVariantsBuilder,
    IAttributes attributes,
    IInstanceDpProvider instanceDpProvider,
    Func<IConstructorInjectionsCounterWalker> constructorInjectionsCounterWalkerFactory,
    ILocationProvider locationProvider,
    IFilter filter,
    ITypeResolver typeResolver,
    IOverloadResolutionPriority overloadResolutionPriority)
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
                    [locationProvider.GetLocation(implementation.Source)],
                    LogId.ErrorCannotConstructAbstractType,
                    nameof(Strings.Error_Template_CannotConstructAbstractType));
            }

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

                if (constructor.Parameters.IsDefaultOrEmpty && setup.Hints.SkipDefaultConstructor)
                {
                    string GetTypeName() => typeResolver.Resolve(ctx.Setup, implementationType).Name;
                    string GetLifetimeName() => binding.Lifetime.ValueToString();
                    if (filter.IsMeet(
                            ctx.Setup,
                            (Hint.SkipDefaultConstructorImplementationTypeNameRegularExpression, Hint.SkipDefaultConstructorImplementationTypeNameWildcard, GetTypeName),
                            (Hint.SkipDefaultConstructorLifetimeRegularExpression, Hint.SkipDefaultConstructorLifetimeWildcard, GetLifetimeName)))
                    {
                        continue;
                    }
                }

                constructors.Add(
                    new DpMethod(
                        constructor,
                        attributes.GetAttribute(setup.SemanticModel, setup.OrdinalAttributes, constructor, AttributeKind.Ordinal, default(int?)),
                        instanceDpProvider.GetParameters(setup, constructor.Parameters, ctx.TypeConstructor),
                        locationProvider));
            }

            if (constructors.Count == 0)
            {
                var error = new CompileErrorException(
                    string.Format(Strings.Error_Template_CannotBeInstantiatedNoAccessibleConstructor, implementationType),
                    [locationProvider.GetLocation(implementation.Source)],
                    LogId.ErrorNoAccessibleConstructor,
                    nameof(Strings.Error_Template_CannotBeInstantiatedNoAccessibleConstructor));

                yield return new DependencyNode(0, binding, ctx.TypeConstructor, Error: error);
            }

            var instanceDp = instanceDpProvider.Get(ctx.OriginalSetup, ctx.TypeConstructor, implementationType);
            var implementations = constructors
                .Select(constructor =>
                    new DpImplementation(
                        implementation,
                        binding,
                        constructor,
                        instanceDp.Methods,
                        instanceDp.Properties,
                        instanceDp.Fields,
                        locationProvider))
                .ToList();

            var implementationsWithOrdinal = implementations
                .Where(i => i.Constructor.Ordinal.HasValue)
                .ToList();

            if (implementationsWithOrdinal.Count > 0)
            {
                foreach (var node in CreateNodes(ctx, setup.SemanticModel.Compilation, injectionsCounter, implementationsWithOrdinal.OrderBy(i => i.Constructor.Ordinal), false))
                {
                    yield return node;
                }

                continue;
            }

            foreach (var node in CreateNodes(ctx, setup.SemanticModel.Compilation, injectionsCounter, implementations, true))
            {
                yield return node;
            }
        }
    }

    private IEnumerable<DependencyNode> CreateNodes(
        DependencyNodeBuildContext ctx,
        Compilation compilation,
        IConstructorInjectionsCounterWalker walker,
        IEnumerable<DpImplementation> implementations,
        bool useOverloadResolutionPriority)
    {
        var candidates = implementations
            .Select(implementation =>
            {
                var priority = 0;
                var isPriorityExplicit = useOverloadResolutionPriority
                                         && overloadResolutionPriority.TryGet(compilation, implementation.Constructor.Method, out priority);
                return (Implementation: implementation, Priority: priority, IsPriorityExplicit: isPriorityExplicit);
            })
            .ToList();
        var usePrimaryConstructorPriority = useOverloadResolutionPriority && candidates.All(i => !i.IsPriorityExplicit);
        return candidates
            .OrderByDescending(i => i.Priority)
            .ThenByDescending(i => GetInjectionsCount(walker, i.Implementation.Constructor))
            .ThenByDescending(i => i.Implementation.Constructor.Method.DeclaredAccessibility)
            .ThenByDescending(i => usePrimaryConstructorPriority && IsPrimaryConstructor(i.Implementation.Constructor.Method))
            .Select(i => i.Implementation)
            .SelectMany(implementationVariantsBuilder.Build)
            .Select((implementation, variantId) => new DependencyNode(variantId, implementation.Binding, ctx.TypeConstructor, Implementation: implementation));
    }

    private static bool IsPrimaryConstructor(IMethodSymbol constructor) =>
        constructor.DeclaringSyntaxReferences.Any(i => i.GetSyntax() is TypeDeclarationSyntax);

    private static int GetInjectionsCount(IConstructorInjectionsCounterWalker walker, in DpMethod constructor)
    {
        walker.VisitConstructor(Unit.Shared, constructor);
        return walker.Count;
    }
}
