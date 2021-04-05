namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal class ResolverMetadata
    {
        public string TargetTypeName;
        public readonly SyntaxNode SetupNode;
        public readonly ICollection<BindingMetadata> Bindings;
        public readonly ICollection<FallbackMetadata> Fallback;
        public readonly ICollection<string> DependsOn;

        public ResolverMetadata(SyntaxNode setupNode, string targetTypeName)
        {
            SetupNode = setupNode;
            TargetTypeName = targetTypeName;
            Bindings = new List<BindingMetadata>();
            Fallback = new List<FallbackMetadata>();
            DependsOn = new List<string> { "DefaultFeature" };
        }
    }
}