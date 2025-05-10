// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf

namespace Pure.DI.Core;

sealed class MetadataValidator(
    ILogger logger,
    IBaseSymbolsProvider baseSymbolsProvider,
    IMarker marker,
    ILocationProvider locationProvider)
    : IValidator<MdSetup>
{
    public bool Validate(MdSetup setup)
    {
        if (setup is { Kind: CompositionKind.Public, Roots.Length: 0 })
        {
            logger.CompileWarning(
                Strings.Warning_NoRoots,
                ImmutableArray.Create(locationProvider.GetLocation(setup.Source)),
                LogId.WarningMetadataDefect);
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
            && (!SyntaxFacts.IsValidIdentifier(setup.Name.ClassName)
                || !IsValidOrEmptyIdentifier(setup.Name.Namespace.Replace('.', '_'))))
        {
            logger.CompileError(
                string.Format(Strings.Error_Template_InvalidCompositionTypeName, setup.Name),
                ImmutableArray.Create(locationProvider.GetLocation(setup.Name.Source ?? setup.Source)),
                LogId.ErrorInvalidMetadata);
            isValid = false;
        }

        foreach (var root in setup.Roots)
        {
            if (IsValidOrEmptyIdentifier(root.Name))
            {
                continue;
            }

            logger.CompileError(
                string.Format(Strings.Error_Template_InvalidRootName, root.Name),
                ImmutableArray.Create(locationProvider.GetLocation(root.Source)),
                LogId.ErrorInvalidMetadata);
            isValid = false;
        }

        foreach (var routeGroups in setup.Roots.GroupBy(root => new Injection(InjectionKind.Root, root.RootType, root.Tag?.Value, root.RootType.Locations)))
        {
            var roots = routeGroups.ToList();
            if (roots.Count <= 1)
            {
                continue;
            }

            foreach (var root in roots.Skip(1))
            {
                logger.CompileError(
                    string.Format(Strings.Error_Template_RootDuplicate, root.Name, roots[0].Name),
                    ImmutableArray.Create(locationProvider.GetLocation(root.Source)),
                    LogId.ErrorInvalidMetadata);
                isValid = false;
            }
        }

        foreach (var accumulator in setup.Accumulators)
        {
            if (marker.IsMarkerBased(setup, accumulator.AccumulatorType))
            {
                logger.CompileError(
                    Strings.Error_AccumulatorTypeCannotBeGenericTypeMarker,
                    ImmutableArray.Create(locationProvider.GetLocation(accumulator.Source)),
                    LogId.ErrorInvalidMetadata);
            }

            if (marker.IsMarkerBased(setup, accumulator.Type))
            {
                logger.CompileError(
                    Strings.Error_AccumulatorCannotAccumulateGenericTypeMarker,
                    ImmutableArray.Create(locationProvider.GetLocation(accumulator.Source)),
                    LogId.ErrorInvalidMetadata);
            }
        }

        if (!isValid)
        {
            throw HandledException.Shared;
        }

        return true;
    }

    private static bool IsValidIdentifier(string identifier) =>
        !string.IsNullOrEmpty(identifier)
        && SyntaxFacts.IsValidIdentifier(identifier);

    private static bool IsValidOrEmptyIdentifier(string identifier) =>
        string.IsNullOrEmpty(identifier)
        || SyntaxFacts.IsValidIdentifier(identifier);

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
                            string.Format(Strings.Error_Template_InvalidArgumentName, arg.ArgName),
                            ImmutableArray.Create(location()),
                            LogId.ErrorInvalidMetadata);
                        isValid = false;
                    }

                    if (arg.Kind == ArgKind.Class && marker.IsMarkerBased(setup, arg.Type))
                    {
                        logger.CompileError(
                            Strings.Error_ClassArgumentTypeCannotBeGenericTypeMarker,
                            ImmutableArray.Create(location()),
                            LogId.ErrorInvalidMetadata);
                    }
                }
            }
        }

        if (implementationType == null || implementationType is IErrorTypeSymbol || semanticModel == null)
        {
            logger.CompileError(
                Strings.Error_InvalidBindingDueToCompilationError,
                ImmutableArray.Create(location()),
                LogId.ErrorInvalidMetadata);
            return false;
        }

        var severityOfNotImplementedContract = setup.Hints.SeverityOfNotImplementedContract;
        if (severityOfNotImplementedContract > DiagnosticSeverity.Hidden)
        {
            var supportedContracts = new HashSet<ITypeSymbol>(baseSymbolsProvider.GetBaseSymbols(implementationType, (_, _) => true), SymbolEqualityComparer.Default)
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
                        logger.CompileError(message, ImmutableArray.Create(location()), LogId.ErrorInvalidMetadata);
                        isValid = false;
                        break;

                    case DiagnosticSeverity.Warning:
                        logger.CompileWarning(message, ImmutableArray.Create(location()), LogId.WarningMetadataDefect);
                        break;

                    case DiagnosticSeverity.Info:
                        logger.CompileInfo(message, ImmutableArray.Create(location()), LogId.InfoMetadataDefect);
                        break;
                }
            }
        }

        return isValid;
    }
}