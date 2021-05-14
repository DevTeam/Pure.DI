namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class EnumerableObjectBuilder : IObjectBuilder
    {
        private readonly IBuildContext _buildContext;
        private readonly ITypeResolver _typeResolver;

        public EnumerableObjectBuilder(
            IBuildContext buildContext,
            ITypeResolver typeResolver)
        {
            _buildContext = buildContext;
            _typeResolver = typeResolver;
        }

        public ExpressionSyntax Build(IBuildStrategy buildStrategy, Dependency dependency)
        {
            if (
                dependency.Implementation.Type is not INamedTypeSymbol namedTypeSymbol
                || namedTypeSymbol.TypeArguments.Length != 1)
            {
                throw new InvalidOperationException($"Invalid enumerable type {dependency.Implementation}.");
            }

            var elementType = namedTypeSymbol.TypeArguments[0];
            var memberKey = new MemberKey($"EnumerableOf{elementType.Name}", dependency);

            var factoryMethod = (MethodDeclarationSyntax)_buildContext.GetOrAddMember(memberKey, () => {
                var yields =
                    from element in _typeResolver.Resolve(new SemanticType(elementType, dependency.Implementation))
                    let objectCreationExpression = buildStrategy.Build(element)
                    select (StatementSyntax)SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement).WithExpression(objectCreationExpression);

                var factoryName = _buildContext.NameService.FindName(memberKey);
                var type = dependency.Implementation.TypeSyntax;
                return SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                    .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                    .AddParameterListParameters()
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                    .AddBodyStatements(yields.DefaultIfEmpty(SyntaxFactory.YieldStatement(SyntaxKind.YieldBreakStatement)).ToArray());
            });

            return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryMethod.Identifier.Text));
        }
    }
}
