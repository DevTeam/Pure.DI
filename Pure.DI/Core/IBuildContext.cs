﻿namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBuildContext
    {
        Compilation Compilation { get; }

        bool IsCancellationRequested { get; }

        ResolverMetadata Metadata { get; }

        void Prepare(Compilation compilation, CancellationToken cancellationToken, ResolverMetadata metadata);

        INameService NameService { get; }

        ITypeResolver TypeResolver { get; }

        IEnumerable<IBindingMetadata> AdditionalBindings { get; }
        
        IEnumerable<MemberDeclarationSyntax> AdditionalMembers { get; }

        IEnumerable<StatementSyntax> FinalizationStatements { get; }
        
        IEnumerable<StatementSyntax> FinalDisposeStatements { get; }

        bool PossibleCircularDependencies { get; set; }

        void AddBinding(IBindingMetadata binding);

        T GetOrAddMember<T>(MemberKey key, Func<T> additionalMemberFactory) where T: MemberDeclarationSyntax;

        void AddFinalizationStatements(IEnumerable<StatementSyntax> finalizationStatement);
        
        void AddFinalDisposeStatements(IEnumerable<StatementSyntax> releaseStatements);
    }
}