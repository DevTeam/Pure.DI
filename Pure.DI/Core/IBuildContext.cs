namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBuildContext
    {
        ResolverMetadata Metadata { get; set; }

        SemanticModel SemanticModel { get; set; }

        void Prepare();

        INameService NameService { get; }

        ITypeResolver TypeResolver { get; }

        IEnumerable<BindingMetadata> AdditionalBindings { get; }
        
        IEnumerable<MemberDeclarationSyntax> AdditionalMembers { get; }

        void AddBinding(BindingMetadata binding);

        MemberDeclarationSyntax GetOrAddMember(MemberKey key, Func<MemberDeclarationSyntax> additionalMemberFactory);
    }
}