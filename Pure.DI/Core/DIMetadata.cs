namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal class DIMetadata
    {
        public readonly SyntaxTree Tree;
        public readonly string Namespace;
        public readonly string TargetTypeName;
        public readonly ICollection<BindingMetadata> Bindings;

        public DIMetadata(SyntaxTree tree, string @namespace, string targetTypeName, ICollection<BindingMetadata> bindings)
        {
            Tree = tree;
            Namespace = @namespace;
            TargetTypeName = targetTypeName;
            Bindings = bindings;
        }
    }
}
