// ReSharper disable IdentifierTypo
// ReSharper disable InvertIf
// ReSharper disable MergeIntoPattern
// ReSharper disable ClassNeverInstantiated.Global
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
        private readonly IBuildContext _buildContext;
        private readonly ICannotResolveExceptionFactory _cannotResolveExceptionFactory;
        private readonly ICache<FactoryKey, SyntaxNode> _cache;
        private Dependency _dependency;
        private IBuildStrategy? _buildStrategy;
        private SyntaxToken _contextIdentifier;
        private ExpressionSyntax? _defaultTag;

        public FactoryRewriter(
            IBuildContext buildContext,
            ICannotResolveExceptionFactory cannotResolveExceptionFactory,
            [Tag(Tags.ContainerScope)] ICache<FactoryKey, SyntaxNode> cache)
            : base(true)
        {
            _buildContext = buildContext;
            _cannotResolveExceptionFactory = cannotResolveExceptionFactory;
            _cache = cache;
        }

        public FactoryRewriter Initialize(
            Dependency dependency,
            IBuildStrategy buildStrategy,
            SyntaxToken contextIdentifier
        )
        {
            _dependency = dependency;
            _buildStrategy = buildStrategy;
            _contextIdentifier = contextIdentifier;
            if (dependency.Implementation.Type is INamedTypeSymbol namedTypeSymbol)
            {
                if(namedTypeSymbol.IsGenericType && namedTypeSymbol.ConstructUnboundGenericType().ToString() == "System.Func<>")
                {
                    _defaultTag = dependency.Tag;
                }
            }
            return this;
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

        public override SyntaxNode? VisitVariableDeclaration(VariableDeclarationSyntax node) =>
            base.VisitVariableDeclaration(node.WithType(ReplaceType(node.Type)));

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault() is not { Identifier: { Text: nameof(IContext.Resolve) } })
            {
                return base.VisitInvocationExpression(node);
            }
            
            var nodeKey = new FactoryKey(_dependency, node.ToString());
            if (_cache.TryGetValue(nodeKey, out SyntaxNode result))
            {
                return result;
            }
            
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
                        var tag = invocationOperation.Arguments.Length == 1 ? invocationOperation.Arguments[0].Value.Syntax as ExpressionSyntax : _defaultTag;
                        var dependencyType = _dependency.TypesMap.ConstructType(new SemanticType(invocationOperation.TargetMethod.ReturnType, semanticModel));
                        var dependency = _buildContext.TypeResolver.Resolve(dependencyType, tag);
                        try
                        {
                            result = ReplaceLambdaByCreateExpression(dependency, dependencyType);
                            _cache.Add(nodeKey, result);
                            return result;
                        }
                        catch (BuildException ex)
                        {
                            if (ex.Id == Diagnostics.Error.CircularDependency)
                            {
                                result = ReplaceLambdaByResolveCall(dependencyType, tag);
                                _cache.Add(nodeKey, result);
                                return result;
                            }

                            throw;
                        }
                    }
                }
            }
            
            return base.VisitInvocationExpression(node);
        }

        private static SyntaxNode ReplaceLambdaByResolveCall(SemanticType dependencyType, ExpressionSyntax? tag)
        {
            var result = SyntaxFactory.InvocationExpression(
                SyntaxFactory.GenericName(nameof(IContext.Resolve))
                    .AddTypeArgumentListArguments(dependencyType));

            if (tag != default)
            {
                result = result.AddArgumentListArguments(SyntaxFactory.Argument(tag));
            }

            return result;
        }

        private SyntaxNode ReplaceLambdaByCreateExpression(Dependency dependency, SemanticType dependencyType)
        {
            var expression = _buildStrategy!.TryBuild(dependency, dependencyType);
            if (expression == null)
            {
                throw _cannotResolveExceptionFactory.Create(dependency.Binding, dependency.Tag, "a factory");
            }

            return expression;
        }

        private void ReplaceTypes(IList<TypeSyntax> args, bool addBinding = false)
        {
            for (var i = 0; i < args.Count; i++)
            {
                args[i] = ReplaceType(args[i], addBinding);
            }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private TypeSyntax ReplaceType(TypeSyntax typeSyntax, bool addBinding = false)
        {
            var typeKey = new FactoryKey(_dependency, typeSyntax.ToString());
            if (_cache.TryGetValue(typeKey, out var result))
            {
                return (TypeSyntax)result;
            }
            
            var semanticModel = typeSyntax.SyntaxTree.GetRoot().GetSemanticModel(_dependency.Implementation);
            var typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;
            SemanticType? sematicType = default;
            switch (typeSymbol)
            {
                case INamedTypeSymbol namedTypeSymbol:
                {
                    var curType = new SemanticType(namedTypeSymbol, _dependency.Implementation);
                    sematicType= _dependency.TypesMap.ConstructType(curType);
                    result = sematicType.TypeSyntax;
                    break;
                }
                
                case IArrayTypeSymbol arrayTypeSymbol:
                {
                    var curType = new SemanticType(arrayTypeSymbol.ElementType, _dependency.Implementation);
                    sematicType = _dependency.TypesMap.ConstructType(curType);
                    result = SyntaxFactory.ArrayType(sematicType).AddRankSpecifiers(SyntaxFactory.ArrayRankSpecifier());
                    break;
                }
                
                default:
                    result = typeSyntax;
                    break;
            }
            
            _cache.Add(typeKey, result);
            if (addBinding && sematicType != default)
            {
                AddBinding(sematicType);
            }

            return (TypeSyntax)result;
        }

        private void AddBinding(SemanticType constructedType)
        {
            var binding = new BindingMetadata(_dependency.Binding, constructedType, null);
            if (_dependency.Tag != null)
            {
                binding.AddTags(_dependency.Tag);
            }

            _buildContext.AddBinding(binding);
        }
        
        internal class FactoryKey
        {
            private readonly Dependency _dependency;
            private readonly string _code;

            public FactoryKey(Dependency dependency, string code)
            {
                _dependency = dependency;
                _code = code;
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                FactoryKey other = (FactoryKey)obj;
                return _dependency.Equals(other._dependency) && _code.Equals(other._code);
            }
            
            // return _type.Equals(other._type, SymbolEqualityComparer.Default) && _code.Equals(other._code);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_dependency.GetHashCode() * 397) ^ _code.GetHashCode();
                }
            }
        }
    }
}