namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal record ResolverMetadata
    {
        public readonly string ComposerTypeName;
        public readonly ClassDeclarationSyntax? Owner;
        public readonly SyntaxNode SetupNode;
        public readonly ICollection<IBindingMetadata> Bindings = new List<IBindingMetadata>();
        public readonly ICollection<string> DependsOn = new HashSet<string> { "DefaultFeature", "AspNetFeature" };
        public readonly ICollection<AttributeMetadata> Attributes = new List<AttributeMetadata>();
        public readonly IDictionary<Setting, string> Settings = new Dictionary<Setting, string>();

        public ResolverMetadata(SyntaxNode setupNode, string composerTypeName, ClassDeclarationSyntax? owner)
        {
            SetupNode = setupNode;
            ComposerTypeName = composerTypeName;
            Owner = owner;
        }

        public void Merge(ResolverMetadata dependency)
        {
            foreach (var dependsOn in dependency.DependsOn)
            {
                DependsOn.Add(dependsOn);
            }

            foreach (var binding in dependency.Bindings)
            {
                Bindings.Add(binding);
            }

            foreach (var attribute in dependency.Attributes)
            {
                Attributes.Add(attribute);
            }

            foreach (var setting in dependency.Settings)
            {
                Settings[setting.Key] = setting.Value;
            }
        }
    }
}