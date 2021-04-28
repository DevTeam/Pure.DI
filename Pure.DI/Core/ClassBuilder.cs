﻿// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ClassBuilder : IClassBuilder
    {
        private readonly IMembersBuilder[] _membersBuilder;
        private readonly IDiagnostic _diagnostic;
        private readonly IBindingsProbe _bindingsProbe;
        private readonly ResolverMetadata _metadata;
        
        public ClassBuilder(
            ResolverMetadata metadata,
            IEnumerable<IMembersBuilder> membersBuilder,
            IDiagnostic diagnostic,
            IBindingsProbe bindingsProbe)
        {
            _metadata = metadata;
            _membersBuilder = membersBuilder.OrderBy(i => i.Order).ToArray();
            _diagnostic = diagnostic;
            _bindingsProbe = bindingsProbe;
        }

        public CompilationUnitSyntax Build(SemanticModel semanticModel)
        {
            var classModifiers = new List<SyntaxToken>();
            if (_metadata.Owner == null)
            {
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
            }
            else
            {
                classModifiers.AddRange(_metadata.Owner.Modifiers);
            }

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.CompilerServices")));

            var originalCompilationUnit = _metadata.SetupNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
            if (originalCompilationUnit != null)
            {
                compilationUnit = compilationUnit.AddUsings(originalCompilationUnit.Usings.ToArray());
            }

            NamespaceDeclarationSyntax? prevNamespaceNode = null;
            foreach (var originalNamespaceNode in _metadata.SetupNode.Ancestors().OfType<NamespaceDeclarationSyntax>().Reverse())
            {
                var namespaceNode = 
                    SyntaxFactory.NamespaceDeclaration(originalNamespaceNode.Name)
                        .AddUsings(originalNamespaceNode.Usings.ToArray());

                prevNamespaceNode = prevNamespaceNode == null ? namespaceNode : prevNamespaceNode.AddMembers(namespaceNode);
            }

            _bindingsProbe.Probe();

            var resolverClass = SyntaxFactory.ClassDeclaration(_metadata.TargetTypeName)
                .WithKeyword(SyntaxFactory.Token(SyntaxKind.ClassKeyword))
                .AddModifiers(classModifiers.ToArray())
                .AddMembers(
                    SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(SyntaxRepo.ContextTypeSyntax)
                                .AddVariables(
                                    SyntaxFactory.VariableDeclarator(SyntaxRepo.SharedContextName)
                                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(SyntaxRepo.ContextTypeSyntax).AddArgumentListArguments()))))
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                            SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                        .WithCommentBefore("// Shared context to use in factories"))
                .AddMembers(_membersBuilder.SelectMany(i => i.BuildMembers(semanticModel)).ToArray())
                .AddMembers(
                    SyntaxFactory.ClassDeclaration(SyntaxRepo.ContextClassName)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword))
                        .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxRepo.IContextTypeSyntax))
                        .AddMembers(
                            SyntaxRepo.TResolveMethodSyntax
                                .AddBodyStatements(
                                    SyntaxFactory.ReturnStatement()
                                        .WithExpression(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.ParseName(_metadata.TargetTypeName),
                                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                                    SyntaxFactory.GenericName(nameof(IContext.Resolve))
                                                        .AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax))))))
                        .AddMembers(
                            SyntaxRepo.TResolveMethodSyntax
                                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(SyntaxRepo.ObjectTypeSyntax))
                                .AddBodyStatements(
                                    SyntaxFactory.ReturnStatement()
                                        .WithExpression(
                                            SyntaxFactory.InvocationExpression(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.ParseName(_metadata.TargetTypeName),
                                                        SyntaxFactory.Token(SyntaxKind.DotToken),
                                                        SyntaxFactory.GenericName(nameof(IContext.Resolve))
                                                            .AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax)))
                                                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")))))))
                .WithPragmaWarningDisable(8625);

            if (prevNamespaceNode != null)
            {
                prevNamespaceNode = prevNamespaceNode.AddMembers(resolverClass);
                compilationUnit = compilationUnit.AddMembers(prevNamespaceNode);
            }
            else
            {
                compilationUnit = compilationUnit.AddMembers(resolverClass);
            }

            var sampleDependency = _metadata.Bindings.LastOrDefault()?.Dependencies.FirstOrDefault()?.ToString() ?? "T";
            _diagnostic.Information(Diagnostics.Generated, $"{_metadata.TargetTypeName} was generated. Please use a method like {_metadata.TargetTypeName}.Resolve<{sampleDependency}>() to create a composition root.", _metadata.Bindings.FirstOrDefault()?.Location);
            return compilationUnit;
        }
    }
}