// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf

namespace Pure.DI.Core;

sealed class MetadataValidator(
    ILogger logger,
    IBaseSymbolsProvider baseSymbolsProvider,
    IMarker marker)
    : IValidator<MdSetup>
{
    public bool Validate(MdSetup setup)
    {
        if (setup is { Kind: CompositionKind.Public, Roots.Length: 0 })
        {
            logger.CompileWarning(Strings.Warning_NoRoots, setup.Source.GetLocation(), LogId.WarningMetadataDefect);
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
            logger.CompileError(string.Format(Strings.Error_Template_InvalidCompositionTypeName, setup.Name), (setup.Name.Source ?? setup.Source).GetLocation(), LogId.ErrorInvalidMetadata);
            isValid = false;
        }

        foreach (var root in setup.Roots)
        {
            if (IsValidOrEmptyIdentifier(root.Name))
            {
                continue;
            }

            logger.CompileError(string.Format(Strings.Error_Template_InvalidRootName, root.Name), root.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            isValid = false;
        }

        foreach (var routeGroups in setup.Roots.GroupBy(root => new Injection(InjectionKind.Root, root.RootType, root.Tag?.Value)))
        {
            var roots = routeGroups.ToList();
            if (roots.Count <= 1)
            {
                continue;
            }

            foreach (var root in roots.Skip(1))
            {
                logger.CompileError(string.Format(Strings.Error_Template_RootDuplicate, root.Name, roots[0].Name), root.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                isValid = false;
            }
        }

        foreach (var accumulator in setup.Accumulators)
        {
            if (marker.IsMarkerBased(setup, accumulator.AccumulatorType))
            {
                logger.CompileError(Strings.Error_AccumulatorTypeCannotBeGenericTypeMarker, accumulator.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            }

            if (marker.IsMarkerBased(setup, accumulator.Type))
            {
                logger.CompileError(Strings.Error_AccumulatorCannotAccumulateGenericTypeMarker, accumulator.Source.GetLocation(), LogId.ErrorInvalidMetadata);
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
        var location = binding.Source.GetLocation();
        if (binding.Implementation is {} implementation)
        {
            semanticModel = implementation.SemanticModel;
            implementationType = implementation.Type;
            location = implementation.Source.GetLocation();
        }
        else
        {
            if (binding.Factory is {} factory)
            {
                semanticModel = factory.SemanticModel;
                implementationType = factory.Type;
                location = factory.Source.GetLocation();
            }
            else
            {
                if (binding.Arg is {} arg)
                {
                    semanticModel = arg.SemanticModel;
                    implementationType = arg.Type;
                    location = arg.Source.GetLocation();
                    if (!IsValidIdentifier(arg.ArgName))
                    {
                        logger.CompileError(string.Format(Strings.Error_Template_InvalidArgumentName, arg.ArgName), location, LogId.ErrorInvalidMetadata);
                        isValid = false;
                    }

                    if (arg.Kind == ArgKind.Class && marker.IsMarkerBased(setup, arg.Type))
                    {
                        logger.CompileError(Strings.Error_ClassArgumentTypeCannotBeGenericTypeMarker, location, LogId.ErrorInvalidMetadata);
                    }
                }
            }
        }

        if (implementationType == null || implementationType is IErrorTypeSymbol || semanticModel == null)
        {
            logger.CompileError(Strings.Error_InvalidBindingDueToCompilationError, location, LogId.ErrorInvalidMetadata);
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
                        logger.CompileError(message, location, LogId.ErrorInvalidMetadata);
                        isValid = false;
                        break;

                    case DiagnosticSeverity.Warning:
                        logger.CompileWarning(message, location, LogId.WarningMetadataDefect);
                        break;

                    case DiagnosticSeverity.Info:
                        logger.CompileInfo(message, location, LogId.InfoMetadataDefect);
                        break;
                }
            }
        }

        return isValid;
    }
}