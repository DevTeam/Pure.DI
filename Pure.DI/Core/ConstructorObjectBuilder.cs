namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ConstructorObjectBuilder : IObjectBuilder
    {
        private readonly IDiagnostic _diagnostic;
        private readonly IBuildContext _buildContext;
        private readonly IConstructorsResolver _constructorsResolver;

        public ConstructorObjectBuilder(
            IDiagnostic diagnostic,
            IBuildContext buildContext, 
            IConstructorsResolver constructorsResolver)
        {
            _diagnostic = diagnostic;
            _buildContext = buildContext;
            _constructorsResolver = constructorsResolver;
        }

        public ExpressionSyntax TryBuild(
            ITypeResolver typeResolver,
            IBindingExpressionStrategy bindingExpressionStrategy,
            TypeResolveDescription typeDescription)
        {
            var ctorExpression = (
                from ctor in _constructorsResolver.Resolve(typeResolver, typeDescription)
                let parameters =
                    from parameter in ctor.Parameters
                    let paramResolveDescription = typeResolver.Resolve(parameter.Type, null)
                    select TryBuildInternal(typeResolver, bindingExpressionStrategy, paramResolveDescription.ObjectBuilder, paramResolveDescription)
                where parameters.All(i => i != null)
                let arguments = SyntaxFactory.SeparatedList(
                    from parameter in parameters
                    select SyntaxFactory.Argument(parameter))
                let objectCreationExpression = CreateObject(typeDescription, arguments)
                select objectCreationExpression
            ).FirstOrDefault();

            if (ctorExpression != null)
            {
                return ctorExpression;
            }

            var message = $"Cannot find an open constructor for {typeDescription}.";
            _diagnostic.Error(Diagnostics.CannotFindCtor, message, typeDescription.Binding.Location);
            return SyntaxFactory.ParseName(message);
        }

        private static ExpressionSyntax CreateObject(TypeResolveDescription typeDescription, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            var typeSyntax = typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel);
            if (typeSyntax.IsKind(SyntaxKind.TupleType))
            {
                return SyntaxFactory.TupleExpression()
                    .WithArguments(arguments);
            }

            return SyntaxFactory.ObjectCreationExpression(typeSyntax)
                .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        }

        private ExpressionSyntax? TryBuildInternal(
            ITypeResolver typeResolver,
            IBindingExpressionStrategy bindingExpressionStrategy,
            IObjectBuilder? objectBuilder,
            TypeResolveDescription typeDescription)
        {
            if (objectBuilder == null)
            {
                return null;
            }

            if (typeDescription.Type is INamedTypeSymbol type)
            {
                var constructedType = typeDescription.TypesMap.ConstructType(type);
                if (!typeDescription.Type.Equals(constructedType, SymbolEqualityComparer.Default))
                {
                    _buildContext.AddBinding(new BindingMetadata(typeDescription.Binding, constructedType));
                }

                if (typeDescription.IsResolved)
                {
                    return bindingExpressionStrategy.TryBuild(constructedType, null);
                }

                var contractType = typeDescription.Type.ToTypeSyntax(typeDescription.SemanticModel);
                return SyntaxFactory.CastExpression(contractType,
                    SyntaxFactory.InvocationExpression(SyntaxFactory.ParseName(nameof(IContext.Resolve)))
                        .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(contractType))));
            }

            if (typeDescription.Type is IArrayTypeSymbol arrayType)
            {
                var arrayTypeDescription = typeResolver.Resolve(arrayType, null);
                if (arrayTypeDescription.IsResolved)
                {
                    return bindingExpressionStrategy.TryBuild(arrayTypeDescription);
                }
            }

            return null;
        }
    }
}