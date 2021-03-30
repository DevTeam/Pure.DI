namespace Pure.DI.Core
{
    using System.Collections.Generic;

    internal class ResolverMetadata
    {
        public readonly string Namespace;
        public readonly string TargetTypeName;
        public readonly ICollection<BindingMetadata> Bindings;

        public ResolverMetadata(string @namespace, string targetTypeName, ICollection<BindingMetadata> bindings)
        {
            Namespace = @namespace;
            TargetTypeName = targetTypeName;
            Bindings = bindings;
        }
    }
}