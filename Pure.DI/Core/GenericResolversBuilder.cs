// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class GenericResolversBuilder: IMembersBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IBuildContext _buildContext;
        private readonly ITypeResolver _typeResolver;
        private readonly IBuildStrategy _buildStrategy;
        private readonly Log<GenericResolversBuilder> _log;
        private readonly ITracer _tracer;
        private readonly IStringTools _stringTools;

        public GenericResolversBuilder(
            ResolverMetadata metadata,
            IBuildContext buildContext,
            ITypeResolver typeResolver,
            IBuildStrategy buildStrategy,
            Log<GenericResolversBuilder> log,
            ITracer tracer,
            IStringTools stringTools)
        {
            _metadata = metadata;
            _buildContext = buildContext;
            _typeResolver = typeResolver;
            _buildStrategy = buildStrategy;
            _log = log;
            _tracer = tracer;
            _stringTools = stringTools;
        }

        public int Order => 2;

        public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
        {
            var resolvingTypes = (
                from binding in _metadata.Bindings.Concat(_buildContext.AdditionalBindings)
                from dependency in binding.Dependencies
                where !binding.GetTags(dependency).Any()
                where !dependency.IsComposedGenericTypeMarker
                group binding by dependency into groups
                let binding = groups.First()
                select groups.Key)
                .ToArray();

            foreach (var resolvingType in resolvingTypes)
            {
                if (_buildContext.IsCancellationRequested)
                {
                    _log.Trace(() => new[] { "Build canceled" });
                    break;
                }
                
                var minAccessibility = GetAccessibility(resolvingType.Type).Min();
                if (minAccessibility < Accessibility.Internal)
                {
                    _tracer.Save();
                    continue;
                }

                var resolvedDependency = _typeResolver.Resolve(resolvingType, null);
                if (!resolvedDependency.IsResolved)
                {
                    _tracer.Save();
                    continue;
                }

                var objectExpression = _buildStrategy.TryBuild(resolvedDependency, resolvingType);
                if (objectExpression == null)
                {
                    _tracer.Save();
                    continue;
                }

                var accessibility = minAccessibility == Accessibility.Public ? SyntaxKind.PublicKeyword : SyntaxKind.InternalKeyword;
                var methodName = GetMethodName(resolvingType.Type);
                methodName = _buildContext.NameService.FindName(new MemberKey(methodName, resolvingType.Type));
                yield return 
                    SyntaxFactory.MethodDeclaration(resolvingType.TypeSyntax, methodName)
                        .AddModifiers(SyntaxFactory.Token(accessibility), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                        .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                        .AddBodyStatements(SyntaxFactory.ReturnStatement(objectExpression));
            }
        }

        private static IEnumerable<Accessibility> GetAccessibility(ISymbol symbol)
        {
            yield return symbol.DeclaredAccessibility == Accessibility.NotApplicable ? Accessibility.Internal : symbol.DeclaredAccessibility;
            switch (symbol)
            {
                case INamedTypeSymbol { IsGenericType: true } namedTypeSymbol:
                {
                    var accessibilitySet = 
                        from typeArg in namedTypeSymbol.TypeArguments
                        from accessibility in GetAccessibility(typeArg)
                        select accessibility;

                    foreach (var accessibility in accessibilitySet)
                    {
                        yield return accessibility;
                    }

                    break;
                }

                case IArrayTypeSymbol arrayTypeSymbol:
                    yield return arrayTypeSymbol.ElementType.DeclaredAccessibility;
                    break;
            }
        }

        private string GetMethodName(ISymbol symbol) => $"Resolve{string.Join(string.Empty, GetParts(symbol))}";

        private IEnumerable<string> GetParts(ISymbol symbol)
        {
            foreach (var part in symbol.ToDisplayParts())
            {
                switch (part.Kind)
                {
                    case SymbolDisplayPartKind.NamespaceName:
                        break;
                    
                    case SymbolDisplayPartKind.Punctuation:
                        break;

                    default:
                        yield return _stringTools.ConvertToTitle(part.ToString());
                        break;
                }
            }
        }
    }
}