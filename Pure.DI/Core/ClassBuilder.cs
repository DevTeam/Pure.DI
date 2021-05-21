// ReSharper disable LoopCanBeConvertedToQuery
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
        private readonly IBuildContext _buildContext;
        private readonly IMemberNameService _memberNameService;
        private readonly ResolverMetadata _metadata;
        
        public ClassBuilder(
            IBuildContext buildContext,
            IMemberNameService memberNameService,
            ResolverMetadata metadata,
            IEnumerable<IMembersBuilder> membersBuilder,
            IDiagnostic diagnostic,
            IBindingsProbe bindingsProbe)
        {
            _buildContext = buildContext;
            _memberNameService = memberNameService;
            _metadata = metadata;
            _membersBuilder = membersBuilder.OrderBy(i => i.Order).ToArray();
            _diagnostic = diagnostic;
            _bindingsProbe = bindingsProbe;
        }

        public CompilationUnitSyntax Build(SemanticModel semanticModel)
        {
            var classModifiers = new List<SyntaxToken>();
            _buildContext.NameService.ReserveName(_metadata.TargetTypeName);
            if (_metadata.Owner == null)
            {
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
            }
            else
            {
                classModifiers.AddRange(_metadata.Owner.Modifiers);
                foreach (var member in _metadata.Owner.Members)
                {
                    switch (member)
                    {
                        case BaseTypeDeclarationSyntax baseTypeDeclarationSyntax:
                            _buildContext.NameService.ReserveName(baseTypeDeclarationSyntax.Identifier.Text);
                            break;
                        
                        case MethodDeclarationSyntax methodDeclarationSyntax:
                            _buildContext.NameService.ReserveName(methodDeclarationSyntax.Identifier.Text);
                            break;
                        
                        case BaseFieldDeclarationSyntax fieldDeclarationSyntax:
                            foreach (var variable in fieldDeclarationSyntax.Declaration.Variables)
                            {
                                _buildContext.NameService.ReserveName(variable.Identifier.Text);
                            }
                            
                            break;
                    }
                }
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

            var contextTypeSyntax = SyntaxFactory.ParseTypeName(_memberNameService.GetName(MemberNameKind.ContextClass));
            var resolverClass = SyntaxFactory.ClassDeclaration(_metadata.TargetTypeName)
                .WithKeyword(SyntaxFactory.Token(SyntaxKind.ClassKeyword))
                .AddModifiers(classModifiers.ToArray())
                .AddMembers(
                    SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(contextTypeSyntax)
                                .AddVariables(
                                    SyntaxFactory.VariableDeclarator(_memberNameService.GetName(MemberNameKind.ContextField))
                                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(contextTypeSyntax).AddArgumentListArguments()))))
                        .AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                            SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                        .WithCommentBefore("// Shared context to use in factories"))
                .AddMembers(_membersBuilder.SelectMany(i => i.BuildMembers(semanticModel)).ToArray())
                .AddMembers(
                    SyntaxFactory.ClassDeclaration(_memberNameService.GetName(MemberNameKind.ContextClass))
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
                .WithPragmaWarningDisable(8600, 8602, 8603, 8604, 8625);

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
            _diagnostic.Information(Diagnostics.Information.Generated, $"{_metadata.TargetTypeName} was generated. Please use a method like {_metadata.TargetTypeName}.Resolve<{sampleDependency}>() to create a composition root.", _metadata.Bindings.FirstOrDefault()?.Location);
            return compilationUnit;
        }
    }
}