namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal readonly struct FallbackMetadata
    {
        public readonly ExpressionSyntax Factory;

        public FallbackMetadata(ExpressionSyntax factory) => Factory = factory;
    }
}
