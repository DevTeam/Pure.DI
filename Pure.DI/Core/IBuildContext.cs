namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBuildContext
    {
        ResolverMetadata Metadata { get; set; }

        void Prepare();

        INameService NameService { get; }

        ITypeResolver TypeResolver { get; }

        IEnumerable<BindingMetadata> AdditionalBindings { get; }
        
        IEnumerable<MemberDeclarationSyntax> AdditionalMembers { get; }

        IEnumerable<StatementSyntax> FinalizationStatements { get; }

        void AddBinding(BindingMetadata binding);

        MemberDeclarationSyntax GetOrAddMember(MemberKey key, Func<MemberDeclarationSyntax> additionalMemberFactory);

        void AddFinalizationStatement(StatementSyntax finalizationStatement);
    }
}