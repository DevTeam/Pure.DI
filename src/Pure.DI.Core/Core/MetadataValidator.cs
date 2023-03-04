// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core;

using System.Diagnostics.CodeAnalysis;

internal class MetadataValidator : IValidator<MdSetup>
{
    private readonly ILogger<MetadataValidator> _logger;

    public MetadataValidator(ILogger<MetadataValidator> logger)
    {
        _logger = logger;
    }

    public void Validate(in MdSetup setup, in CancellationToken cancellationToken)
    {
        foreach (var binding in setup.Bindings)
        {
            Validate(binding, cancellationToken);
        }
    }

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
    private void Validate(in MdBinding binding, in CancellationToken cancellationToken)
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
        
        var supportedContracts = new HashSet<ITypeSymbol>(implementationType.AllInterfaces, SymbolEqualityComparer.Default) { implementationType };
        var notSupportedContractsBuilder = ImmutableArray.CreateBuilder<MdContract>();
        foreach (var contract in binding.Contracts)
        {
            if (!supportedContracts.Contains(contract.ContractType))
            {
                notSupportedContractsBuilder.Add(contract);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        var notSupportedContracts = notSupportedContractsBuilder.SafeMoveToImmutable();
        if (notSupportedContracts.Length <= 0)
        {
            return;
        }

        var itemsBuilder = ImmutableArray.CreateBuilder<string>();
        notSupportedContracts.ToBuilder(itemsBuilder, i => i.ToString());
        var items = itemsBuilder.Join(", ");
        _logger.CompileError($"{implementationType} does not implement {items}.", location, LogId.ErrorInvalidMetadata);
        throw HandledException.Shared;
    }
}