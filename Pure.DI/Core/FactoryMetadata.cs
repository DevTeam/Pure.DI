namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class FactoryMetadata
    {
        public readonly ExpressionSyntax Factory;

        public FactoryMetadata(ExpressionSyntax factory) => Factory = factory;
    }
}
