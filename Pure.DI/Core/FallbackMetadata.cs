namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class FallbackMetadata
    {
        public readonly ExpressionSyntax Factory;

        public FallbackMetadata(ExpressionSyntax factory) => Factory = factory;
    }
}
