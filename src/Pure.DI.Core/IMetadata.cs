namespace Pure.DI;

public interface IMetadata
{
    bool IsMetadata(SyntaxNode node, SemanticModel? semanticModel, CancellationToken cancellationToken);
}