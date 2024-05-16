// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf
namespace Pure.DI.Core;

internal sealed class MetadataValidator(
    ILogger<MetadataValidator> logger,
    IBaseSymbolsProvider baseSymbolsProvider,
    IMarker marker)
    : IValidator<MdSetup>
{
    public bool Validate(MdSetup setup)
    {
        if (setup.Kind == CompositionKind.Public && !setup.Roots.Any())
        {
            logger.CompileWarning("None of the composition roots are declared. Add at least one root.", setup.Source.GetLocation(), LogId.WarningMetadataDefect);
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
            logger.CompileError($"Invalid composition type name \"{setup.Name}\".", (setup.Name.Source ?? setup.Source).GetLocation(), LogId.ErrorInvalidMetadata);
            isValid = false;
        }

        foreach (var root in setup.Roots)
        {
            if (IsValidOrEmptyIdentifier(root.Name))
            {
                continue;
            }

            logger.CompileError($"Invalid root name \"{root.Name}\".", root.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            isValid = false;
        }

        foreach (var routeGroups in setup.Roots.GroupBy(root => new Injection(root.RootType, root.Tag?.Value)))
        {
            var roots = routeGroups.ToArray();
            if (roots.Length <= 1)
            {
                continue;
            }

            foreach (var root in roots.Skip(1))
            {
                logger.CompileError($"The composition root \"{root.Name}\" duplicates the previously declared root \"{roots[0].Name}\".", root.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                isValid = false;
            }
        }

        foreach (var accumulator in setup.Accumulators)
        {
            if (marker.IsMarkerBased(accumulator.AccumulatorType))
            {
                logger.CompileError("Accumulator based on marker type is not supported.", accumulator.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            }
            
            if (marker.IsMarkerBased(accumulator.Type))
            {
                logger.CompileError("The accumulator cannot accumulate instances based on marker type.", accumulator.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            }
        }

        if (!isValid)
        {
            throw HandledException.Shared;
        }

        return true;
    }

    private static bool IsValidOrEmptyIdentifier(string identifier) => 
        string.IsNullOrEmpty(identifier)
        || SyntaxFacts.IsValidIdentifier(identifier);

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
    private bool Validate(MdSetup setup, in MdBinding binding)
    {
        var isValid = true;
        ITypeSymbol? implementationType = default;
        SemanticModel? semanticModel = default;
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
                    if (!SyntaxFacts.IsValidIdentifier(arg.ArgName))
                    {
                        logger.CompileError($"Invalid argument name \"{arg.ArgName}\".", location, LogId.ErrorInvalidMetadata);
                        isValid = false;
                    }
                }
            }
        }
        
        if (implementationType == default || implementationType is IErrorTypeSymbol || semanticModel == default)
        {
            logger.CompileError("Invalid binding due to construction failure.", location, LogId.ErrorInvalidMetadata);
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
                .ToArray();

            // ReSharper disable once InvertIf
            if (notSupportedContracts.Any())
            {
                var message = $"{implementationType} does not implement {string.Join(", ", notSupportedContracts.Select(i => i.ToString()))}.";
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