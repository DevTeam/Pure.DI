namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal class ResolverMetadata
    {
        public string TargetTypeName;
        public readonly SyntaxNode SetupNode;
        public readonly ICollection<BindingMetadata> Bindings;
        public readonly ICollection<FactoryMetadata> Factories;

        public ResolverMetadata(SyntaxNode setupNode, string targetTypeName)
        {
            SetupNode = setupNode;
            TargetTypeName = targetTypeName;
            Bindings = new List<BindingMetadata>();
            Factories = new List<FactoryMetadata>();
        }
    }
}