// ReSharper disable IdentifierTypo
// ReSharper disable InvertIf
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Operations;

    internal class FactoryRewriter: CSharpSyntaxRewriter
    {
        private readonly Dependency _dependency;
        private readonly IBuildStrategy _buildStrategy;
        private readonly SyntaxToken _contextIdentifier;
        private readonly IBuildContext _buildContext;
        private readonly ICannotResolveExceptionFactory _cannotResolveExceptionFactory;
        
        public FactoryRewriter(
            Dependency dependency,
            IBuildStrategy buildStrategy,
            SyntaxToken contextIdentifier,
            IBuildContext buildContext,
            ICannotResolveExceptionFactory cannotResolveExceptionFactory)
            : base(true)
        {
            _dependency = dependency;
            _buildStrategy = buildStrategy;
            _contextIdentifier = contextIdentifier;
            _buildContext = buildContext;
            _cannotResolveExceptionFactory = cannotResolveExceptionFactory;
        }

        public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            var args = node.Arguments.ToArray();
            ReplaceTypes(args);
            return SyntaxFactory.TypeArgumentList().AddArguments(args);
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            if (node.IsUnboundGenericName)
            {
                return node;
            }

            var args = node.TypeArgumentList.Arguments.ToArray();
            ReplaceTypes(args);
            return SyntaxFactory.GenericName(node.Identifier).AddTypeArgumentListArguments(args);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (
                node.Kind() == SyntaxKind.SimpleMemberAccessExpression
                && node.Expression is IdentifierNameSyntax identifierName
                && identifierName.ToString() == _contextIdentifier.Text)
            {
                var method = node.ChildNodes().OfType<GenericNameSyntax>().FirstOrDefault();
                if (method != null)
                {
                    var args = method.TypeArgumentList.Arguments.ToArray();
                    ReplaceTypes(args, true);
                    if (_dependency.Binding.AnyTag && _dependency.Tag != null)
                    {
                        return SyntaxFactory.ParenthesizedLambdaExpression(
                            SyntaxFactory.InvocationExpression((GenericNameSyntax) VisitGenericName(method))
                                .AddArgumentListArguments(SyntaxFactory.Argument(_dependency.Tag)));
                    }
                }

                return Visit(method);
            }

            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node is GenericNameSyntax genericName)
            {
                var args = genericName.TypeArgumentList.Arguments.ToArray();
                ReplaceTypes(args);
                return SyntaxFactory.GenericName(genericName.Identifier).AddTypeArgumentListArguments(args);
            }

            return base.Visit(node);
        }

        public override SyntaxNode VisitTypeOfExpression(TypeOfExpressionSyntax node) => 
            SyntaxFactory.TypeOfExpression(ReplaceType(node.Type));

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var semanticModel = node.GetSemanticModel(_dependency.Implementation);
            var operation = semanticModel.GetOperation(node);
            if (operation is IInvocationOperation invocationOperation)
            {
                if (
                    invocationOperation.TargetMethod.IsGenericMethod
                    && invocationOperation.TargetMethod.TypeArguments.Length == 1)
                {
                    if (invocationOperation.TargetMethod.Name == nameof(IContext.Resolve)
                        && new SemanticType(invocationOperation.TargetMethod.ContainingType, _dependency.Implementation.SemanticModel).Equals(typeof(IContext))
                        && SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.TypeArguments[0], invocationOperation.TargetMethod.ReturnType))
                    {
                        var tag = invocationOperation.Arguments.Length == 1 ? invocationOperation.Arguments[0].Value.Syntax as ExpressionSyntax : null;
                        var dependencyType = _dependency.TypesMap.ConstructType(new SemanticType(invocationOperation.TargetMethod.ReturnType, semanticModel));
                        var dependency = _buildContext.TypeResolver.Resolve(dependencyType, tag);
                        var expression = _buildStrategy.TryBuild(dependency, dependencyType);
                        if (expression == null)
                        {
                            throw _cannotResolveExceptionFactory.Create(dependency.Binding);
                        }

                        return expression;
                    }
                }
            }

            return base.VisitInvocationExpression(node);
        }

        private void ReplaceTypes(IList<TypeSyntax> args, bool addBinding = false)
        {
            for (var i = 0; i < args.Count; i++)
            {
                args[i] = ReplaceType(args[i], addBinding);
            }
        }

        private TypeSyntax ReplaceType(TypeSyntax typeSyntax, bool addBinding = false)
        {
            var semanticModel = typeSyntax.SyntaxTree.GetRoot().GetSemanticModel(_dependency.Implementation);
            var typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;
            switch (typeSymbol)
            {
                case INamedTypeSymbol namedTypeSymbol:
                {
                    var curType = new SemanticType(namedTypeSymbol, _dependency.Implementation);
                    var constructedType = _dependency.TypesMap.ConstructType(curType);
                    if (addBinding)
                    {
                        AddBinding(constructedType);
                    }

                    return constructedType.TypeSyntax;
                }
                
                case IArrayTypeSymbol arrayTypeSymbol:
                {
                    var curType = new SemanticType(arrayTypeSymbol.ElementType, _dependency.Implementation);
                    var constructedType = _dependency.TypesMap.ConstructType(curType);
                    if (addBinding)
                    {
                        AddBinding(constructedType);
                    }

                    return SyntaxFactory.ArrayType(constructedType).AddRankSpecifiers(SyntaxFactory.ArrayRankSpecifier());
                }
                
                default:
                    return typeSyntax;
            }
        }

        private void AddBinding(SemanticType constructedType)
        {
            var binding = new BindingMetadata(_dependency.Binding, constructedType, null);
            if (_dependency.Tag != null)
            {
                binding.Tags.Add(_dependency.Tag);
            }

            _buildContext.AddBinding(binding);
        }
    }
}