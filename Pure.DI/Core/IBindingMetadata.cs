namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBindingMetadata
    {
        object Id { get; }
        
        Location? Location { get; }

        SemanticType? Implementation { get; }

        SimpleLambdaExpressionSyntax? Factory { get; }

        Lifetime Lifetime { get; }

        bool AnyTag { get; }

        bool FromProbe { get; }

        IEnumerable<SemanticType> Dependencies { get; }
        
        IEnumerable<ExpressionSyntax> Tags  { get; }

        IEnumerable<ExpressionSyntax> GetTags(SemanticType dependencyType);
    }
}