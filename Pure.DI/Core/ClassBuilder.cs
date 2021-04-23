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
            var ownerClass = (
                from cls in _metadata.SetupNode.Ancestors().OfType<ClassDeclarationSyntax>()
                where
                    cls.Modifiers.Any(i => i.Kind() == SyntaxKind.StaticKeyword)
                    && cls.Modifiers.Any(i => i.Kind() == SyntaxKind.PartialKeyword)
                    && cls.Modifiers.All(i => i.Kind() != SyntaxKind.PrivateKeyword)
                select cls).FirstOrDefault();

            var classModifiers = new List<SyntaxToken>();
            if (ownerClass == null)
            {
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                if (string.IsNullOrWhiteSpace(_metadata.TargetTypeName))
                {
                    var parentNodeName = _metadata.SetupNode
                        .Ancestors()
                        .Select(TryGetNodeName)
                        .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
                    
                    _metadata.TargetTypeName = $"{parentNodeName}DI";
                    _diagnostic.Information(Diagnostics.CannotUseCurrentType, $"It is not possible to use the current type as DI. Please make sure it is static partial and has public or internal access modifiers. {_metadata.TargetTypeName} will be used instead. You may change this name by passing the optional argument to DI.Setup(string targetTypeName).", _metadata.Bindings.FirstOrDefault()?.Location);
                }
            }
            else
            {
                classModifiers.AddRange(ownerClass.Modifiers);
                if (string.IsNullOrWhiteSpace(_metadata.TargetTypeName))
                {
                    _metadata.TargetTypeName = ownerClass.Identifier.Text;
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

            var pragmaWarningDisable8625 = SyntaxFactory.PragmaWarningDirectiveTrivia(
                SyntaxFactory.Token(SyntaxKind.DisableKeyword),
                SyntaxFactory.SeparatedList<ExpressionSyntax>()
                    .Add(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(8625))),
            false);

            var resolverClass = SyntaxFactory.ClassDeclaration(_metadata.TargetTypeName)
                .WithKeyword(SyntaxFactory.Token(SyntaxKind.ClassKeyword))
                .WithLeadingTrivia(SyntaxFactory.TriviaList().Add(SyntaxFactory.Trivia(pragmaWarningDisable8625)))
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
                                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
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
                                                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")))))));

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
            return compilationUnit.NormalizeWhitespace();
        }

        private static string? TryGetNodeName(SyntaxNode node) =>
            node switch
            {
                ClassDeclarationSyntax classDeclaration => classDeclaration.Identifier.Text,
                StructDeclarationSyntax structDeclaration => structDeclaration.Identifier.Text,
                RecordDeclarationSyntax recordDeclaration => recordDeclaration.Identifier.Text,
                _ => null
            };
    }
}