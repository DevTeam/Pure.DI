namespace Pure.DI.Core;

using NS35EBD81B;

internal interface IBindingMetadata
{
    object Id { get; }

    Location? Location { get; }

    SemanticType? Implementation { get; }

    SimpleLambdaExpressionSyntax? Factory { get; }

    Lifetime Lifetime { get; }

    bool AnyTag { get; }

    BindingType BindingType { get; }

    IEnumerable<SemanticType> Dependencies { get; }

    IEnumerable<ExpressionSyntax> GetTags(SemanticType dependencyType);
}