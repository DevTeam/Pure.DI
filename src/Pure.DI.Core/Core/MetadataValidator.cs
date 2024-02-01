// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class MetadataValidator(
    ILogger<MetadataValidator> logger)
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
                logger.CompileError($"The composition root \"{root.Name}\" duplicates the previously declared root \"{roots[0].Name}\"", root.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                isValid = false;
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
        
        var supportedContracts = new HashSet<ITypeSymbol>(GetBaseTypes(implementationType), SymbolEqualityComparer.Default) { implementationType };
        var notSupportedContracts = binding.Contracts
            .Where(contract => !supportedContracts.Contains(contract.ContractType))
            .Select(i => i.ContractType)
            .ToArray();

        // ReSharper disable once InvertIf
        if (notSupportedContracts.Any())
        {
            var message = $"{implementationType} does not implement {string.Join(", ", notSupportedContracts.Select(i => i.ToString()))}.";
            switch (setup.Hints.GetHint(Hint.SeverityOfNotImplementedContract, DiagnosticSeverity.Error))
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


                case DiagnosticSeverity.Hidden:
                default:
                    break;
            }
        }

        return isValid;
    }
    
    private static IEnumerable<ITypeSymbol> GetBaseTypes(ITypeSymbol symbol)
    {
        while (true)
        {
            yield return symbol;
            foreach (var type in symbol.AllInterfaces.SelectMany(GetBaseTypes))
            {
                yield return type;
            }

            if (symbol.BaseType != default)
            {
                symbol = symbol.BaseType;
                continue;
            }

            break;
        }
    }
}