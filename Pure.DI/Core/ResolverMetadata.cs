namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class ResolverMetadata
    {
        public readonly string Namespace;
        public readonly IReadOnlyCollection<UsingDirectiveSyntax> UsingDirectives;
        public readonly string TargetTypeName;
        public readonly ICollection<BindingMetadata> Bindings;

        public ResolverMetadata(string @namespace, IEnumerable<UsingDirectiveSyntax> usingDirectives, string targetTypeName)
        {
            Namespace = @namespace;
            UsingDirectives = new List<UsingDirectiveSyntax>(usingDirectives);
            TargetTypeName = targetTypeName;
            Bindings = new List<BindingMetadata>();
        }
    }
}