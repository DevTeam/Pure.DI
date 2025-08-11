namespace Pure.DI.Core.Code.Parts;

interface IRootSignatureProvider
{
    string GetRootSignature(CompositionCode composition, Root root);

    public ImmutableDictionary<TypeDescription, ImmutableArray<string>>? TryGetConstraints(CompositionCode composition, Root root);
}