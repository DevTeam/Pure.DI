namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal class ResolverMetadata
    {
        public string TargetTypeName;
        public readonly SyntaxNode SetupNode;
        public readonly ICollection<BindingMetadata> Bindings = new List<BindingMetadata>();
        public readonly ICollection<FallbackMetadata> Fallback = new List<FallbackMetadata>();
        public readonly ICollection<string> DependsOn = new List<string> { "DefaultFeature" };
        public readonly ICollection<AttributeMetadata> Attributes = new List<AttributeMetadata>();

        public ResolverMetadata(SyntaxNode setupNode, string targetTypeName)
        {
            SetupNode = setupNode;
            TargetTypeName = targetTypeName;
        }
    }
}