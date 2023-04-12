// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Diagnostics.CodeAnalysis;

internal class MetadataValidator : IValidator<MdSetup>
{
    private readonly ILogger<MetadataValidator> _logger;

    public MetadataValidator(ILogger<MetadataValidator> logger)
    {
        _logger = logger;
    }

    public void Validate(in MdSetup setup, CancellationToken cancellationToken)
    {
        if (setup.Kind == CompositionKind.Public && !setup.Roots.Any())
        {
            _logger.CompileWarning("None of the composition roots are declared. Add at least one root.", setup.Source.GetLocation(), LogId.WarningMetadataDefect);
        }

        var isValid = setup.Bindings
            .Aggregate(
                true, 
                (current, binding) => current & Validate(binding, cancellationToken));

        foreach (var routeGroups in setup.Roots.GroupBy(root => new Injection(root.RootType, root.Tag?.Value)))
        {
            var roots = routeGroups.ToArray();
            if (roots.Length <= 1)
            {
                continue;
            }

            foreach (var root in roots.Skip(1))
            {
                _logger.CompileError($"The composition root \"{root.Name}\" duplicates the previously declared root \"{roots[0].Name}\"", root.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                isValid = false;
            }
        }

        if (!isValid)
        {
            throw HandledException.Shared;
        }
    }

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
    private bool Validate(in MdBinding binding, in CancellationToken cancellationToken)
    {
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
                }
            }
        }
        
        if (implementationType == default || semanticModel == default)
        {
            _logger.CompileError("Invalid binding due to construction failure.", location, LogId.ErrorInvalidMetadata);
            throw HandledException.Shared;
        }
        
        var supportedContracts = new HashSet<ITypeSymbol>(GetBaseTypes(implementationType), SymbolEqualityComparer.Default) { implementationType };
        var notSupportedContracts = binding.Contracts
            .Where(contract => !supportedContracts.Contains(contract.ContractType))
            .Select(i => i.ContractType)
            .ToArray();
        
        if (!notSupportedContracts.Any())
        {
            return true;
        }

        _logger.CompileError($"{implementationType} does not implement {string.Join(", ", notSupportedContracts.Select(i => i.ToString()))}.", location, LogId.ErrorInvalidMetadata);
        return false;
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