// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf

#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.

namespace Pure.DI.Core;

sealed class MetadataValidator(
    ILogger logger,
    IBaseSymbolsProvider baseSymbolsProvider,
    IMarker marker,
    ILocationProvider locationProvider,
    ITypeSymbolComparer typeSymbolComparer)
    : IValidator<MdSetup>
{
    public bool Validate(MdSetup setup)
    {
        if (setup is { Kind: CompositionKind.Public, Roots.Length: 0 })
        {
            logger.CompileWarning(
                LogMessage.From(nameof(Strings.Warning_NoRoots), Strings.Warning_NoRoots),
                ImmutableArray.Create(locationProvider.GetLocation(setup.Source)),
                LogId.WarningNoRoots);
        }

        var isValid = setup.Bindings
            .Aggregate(
                true,
                (current, binding) => current & Validate(setup, binding));

        if (!isValid)
        {
            return false;
        }

        if (setup.Kind == CompositionKind.Public
            && (!IsValidCompositionTypeName(setup.Name.ClassName)
                || !IsValidOrEmptyIdentifier(setup.Name.Namespace.Replace('.', '_'))))
        {
            logger.CompileError(
                LogMessage.Format(
                    nameof(Strings.Error_Template_InvalidCompositionTypeName),
                    Strings.Error_Template_InvalidCompositionTypeName,
                    setup.Name),
                ImmutableArray.Create(locationProvider.GetLocation(setup.Name.Source ?? setup.Source)),
                LogId.ErrorInvalidCompositionTypeName);
            isValid = false;
        }

        foreach (var root in setup.Roots)
        {
            if (IsValidOrEmptyIdentifier(root.Name))
            {
                continue;
            }

            logger.CompileError(
                LogMessage.Format(
                    nameof(Strings.Error_Template_InvalidRootName),
                    Strings.Error_Template_InvalidRootName,
                    root.Name),
                ImmutableArray.Create(locationProvider.GetLocation(root.Source)),
                LogId.ErrorInvalidRootName);
            isValid = false;
        }

        foreach (var routeGroups in setup.Roots.GroupBy(root => new Injection(InjectionKind.Root, RefKind.None, root.RootType, root.Tag?.Value, root.RootType.Locations)))
        {
            var roots = routeGroups.ToList();
            if (roots.Count <= 1)
            {
                continue;
            }

            foreach (var root in roots.Skip(1))
            {
                logger.CompileError(
                    LogMessage.Format(
                        nameof(Strings.Error_Template_RootDuplicate),
                        Strings.Error_Template_RootDuplicate,
                        root.Name,
                        roots[0].Name),
                    ImmutableArray.Create(locationProvider.GetLocation(root.Source)),
                    LogId.ErrorDuplicateRootName);
                isValid = false;
            }
        }

        foreach (var accumulator in setup.Accumulators)
        {
            if (marker.IsMarkerBased(setup, accumulator.AccumulatorType))
            {
                logger.CompileError(
                    LogMessage.From(
                        nameof(Strings.Error_AccumulatorTypeCannotBeGenericTypeMarker),
                        Strings.Error_AccumulatorTypeCannotBeGenericTypeMarker),
                    ImmutableArray.Create(locationProvider.GetLocation(accumulator.Source)),
                    LogId.ErrorAccumulatorTypeGenericMarker);
            }

            if (marker.IsMarkerBased(setup, accumulator.Type))
            {
                logger.CompileError(
                    LogMessage.From(
                        nameof(Strings.Error_AccumulatorCannotAccumulateGenericTypeMarker),
                        Strings.Error_AccumulatorCannotAccumulateGenericTypeMarker),
                    ImmutableArray.Create(locationProvider.GetLocation(accumulator.Source)),
                    LogId.ErrorAccumulatorCannotAccumulateGenericMarker);
            }
        }

        foreach (var specialType in setup.SpecialTypes)
        {
            if (marker.IsMarkerBased(setup, specialType.Type))
            {
                logger.CompileError(
                    LogMessage.From(
                        nameof(Strings.Error_SpecialTypeCannotBeGenericTypeMarker),
                        Strings.Error_SpecialTypeCannotBeGenericTypeMarker),
                    ImmutableArray.Create(specialType.Source.GetLocation()),
                    LogId.ErrorSpecialTypeGenericMarker);
            }
        }

        return isValid ? true : throw HandledException.Shared;
    }

    private static bool IsValidIdentifier(string identifier) =>
        !string.IsNullOrEmpty(identifier)
        && SyntaxFacts.IsValidIdentifier(identifier);

    private static bool IsValidOrEmptyIdentifier(string identifier) =>
        string.IsNullOrEmpty(identifier)
        || SyntaxFacts.IsValidIdentifier(identifier);

    private static bool IsValidCompositionTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        var nameSyntax = SyntaxFactory.ParseName(typeName);
        return !nameSyntax.ContainsDiagnostics && nameSyntax.DescendantTokens().All(token => !token.IsMissing);
    }

    private bool Validate(MdSetup setup, in MdBinding binding)
    {
        var isValid = true;
        ITypeSymbol? implementationType = null;
        SemanticModel? semanticModel = null;
        var bindingCopy = binding;
        var location = () => locationProvider.GetLocation(bindingCopy.Source);
        if (binding.Implementation is {} implementation)
        {
            semanticModel = implementation.SemanticModel;
            implementationType = implementation.Type;
            location = () => locationProvider.GetLocation(implementation.Source);
        }
        else
        {
            if (binding.Factory is {} factory)
            {
                semanticModel = factory.SemanticModel;
                implementationType = factory.Type;
                location = () => locationProvider.GetLocation(factory.Source);
            }
            else
            {
                if (binding.Arg is {} arg)
                {
                    semanticModel = arg.SemanticModel;
                    implementationType = arg.Type;
                    location = () => locationProvider.GetLocation(arg.Source);
                    if (!IsValidIdentifier(arg.ArgName))
                    {
                        logger.CompileError(
                            LogMessage.Format(
                                nameof(Strings.Error_Template_InvalidArgumentName),
                                Strings.Error_Template_InvalidArgumentName,
                                arg.ArgName),
                            ImmutableArray.Create(location()),
                            LogId.ErrorInvalidArgumentName);
                        isValid = false;
                    }

                    if (arg.Kind == ArgKind.Composition && marker.IsMarkerBased(setup, arg.Type))
                    {
                        logger.CompileError(
                            LogMessage.From(
                                nameof(Strings.Error_CompositionArgumentTypeCannotBeGenericTypeMarker),
                                Strings.Error_CompositionArgumentTypeCannotBeGenericTypeMarker),
                            ImmutableArray.Create(location()),
                            LogId.ErrorCompositionArgGenericMarker);
                    }
                }
            }
        }

        if (implementationType == null || implementationType is IErrorTypeSymbol || semanticModel == null)
        {
            logger.CompileError(
                LogMessage.From(
                    nameof(Strings.Error_InvalidBindingDueToCompilationError),
                    Strings.Error_InvalidBindingDueToCompilationError),
                ImmutableArray.Create(location()),
                LogId.ErrorInvalidBindingDueToCompilation);
            return false;
        }

        var severityOfNotImplementedContract = setup.Hints.SeverityOfNotImplementedContract;
        if (severityOfNotImplementedContract > DiagnosticSeverity.Hidden)
        {
            var supportedContracts = new HashSet<ITypeSymbol>(baseSymbolsProvider.GetBaseSymbols(implementationType, (_, _) => true).Select(i => i.Type), typeSymbolComparer.Runtime)
            {
                implementationType
            };

            var notSupportedContracts = binding.Contracts
                .Where(contract => contract.ContractType != null && !supportedContracts.Contains(contract.ContractType))
                .Select(i => i.ContractType!)
                .ToList();

            // ReSharper disable once InvertIf
            if (notSupportedContracts.Any())
            {
                var message = string.Format(Strings.Error_Template_NotImplement, implementationType, string.Join(", ", notSupportedContracts.Select(i => i.ToString())));
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (severityOfNotImplementedContract)
                {
                    case DiagnosticSeverity.Error:
                        logger.CompileError(
                            LogMessage.From(nameof(Strings.Error_Template_NotImplement), message),
                            ImmutableArray.Create(location()),
                            LogId.ErrorNotImplementedContract);
                        isValid = false;
                        break;

                    case DiagnosticSeverity.Warning:
                        logger.CompileWarning(
                            LogMessage.From(nameof(Strings.Error_Template_NotImplement), message),
                            ImmutableArray.Create(location()),
                            LogId.WarningNotImplementedContract);
                        break;

                    case DiagnosticSeverity.Info:
                        logger.CompileInfo(
                            LogMessage.From(nameof(Strings.Error_Template_NotImplement), message),
                            ImmutableArray.Create(location()),
                            LogId.InfoNotImplementedContract);
                        break;
                }
            }
        }

        return isValid;
    }
}
