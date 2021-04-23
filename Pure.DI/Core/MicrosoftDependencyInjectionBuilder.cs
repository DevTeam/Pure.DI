namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class MicrosoftDependencyInjectionBuilder: IMembersBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IBuildContext _buildContext;
        private readonly ITypeResolver _typeResolver;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IDiagnostic _diagnostic;

        public MicrosoftDependencyInjectionBuilder(
            ResolverMetadata metadata,
            IBuildContext buildContext,
            ITypeResolver typeResolver,
            [Tag(Tags.SimpleBuildStrategy)] IBuildStrategy buildStrategy,
            IDiagnostic diagnostic)
        {
            _metadata = metadata;
            _buildContext = buildContext;
            _typeResolver = typeResolver;
            _buildStrategy = buildStrategy;
            _diagnostic = diagnostic;
        }

        public int Order => 1;

        public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
        {
            var dependencies = (
                from binding in _metadata.Bindings.Concat(_buildContext.AdditionalBindings)
                where !binding.Tags.Any()
                from dependency in binding.Dependencies
                where dependency.Type.IsReferenceType && !dependency.IsComposedGenericTypeMarker
                group binding by dependency into groups    
                let binding = groups.First()
                select (dependency: groups.Key, lifetime: binding.Lifetime, binding)).ToArray();

            var serviceCollectionType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
            var serviceDescriptorType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceDescriptor");
            var mvcBuilderType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IMvcBuilder");
            var mvcServiceCollectionExtensionsType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.MvcServiceCollectionExtensions");
            var serviceCollectionDescriptorExtensionsType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions");
            var controllerFeatureType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controllers.ControllerFeature");
            var controllerActivatorType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator");
            var serviceBasedControllerActivatorType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controllers.ServiceBasedControllerActivator");
            var serviceCollectionServiceExtensionsType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions");
            
            if (
                serviceCollectionType == null
                || serviceDescriptorType == null
                || mvcBuilderType == null
                || mvcServiceCollectionExtensionsType == null
                || serviceCollectionDescriptorExtensionsType == null
                || controllerFeatureType == null
                || controllerActivatorType == null
                || serviceBasedControllerActivatorType == null
                || serviceCollectionServiceExtensionsType == null)
            {
                foreach (var (dependency, lifetime, binding) in dependencies.Where(i => i.lifetime == Lifetime.Scoped || i.lifetime == Lifetime.ContainerSingleton))
                {
                    var error = $"Impossible to use the lifetime {lifetime} for {dependency} outside an ASP.NET context.";
                    _diagnostic.Error(Diagnostics.Unsupported, error, binding.Location);
                }

                yield break;
            }

            var serviceCollection = new SemanticType(serviceCollectionType, semanticModel);
            var serviceDescriptor = new SemanticType(serviceDescriptorType, semanticModel);
            var mvcBuilder = new SemanticType(mvcBuilderType, semanticModel);
            var mvcServiceCollectionExtensions = new SemanticType(mvcServiceCollectionExtensionsType, semanticModel);
            var serviceCollectionDescriptorExtensions = new SemanticType(serviceCollectionDescriptorExtensionsType, semanticModel);
            var controllerFeature = new SemanticType(controllerFeatureType, semanticModel);
            var controllerActivator = new SemanticType(controllerActivatorType, semanticModel);
            var serviceBasedControllerActivator = new SemanticType(serviceBasedControllerActivatorType, semanticModel);
            var serviceCollectionServiceExtensions = new SemanticType(serviceCollectionServiceExtensionsType, semanticModel);
            
            var thisParameter = SyntaxFactory.Parameter(
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ThisKeyword)), 
                serviceCollection,
                SyntaxFactory.Identifier("services"),
                null);

            var method = SyntaxFactory.MethodDeclaration(serviceCollection, $"Add{_metadata.TargetTypeName}")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(thisParameter);

            var getBuilder = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        mvcServiceCollectionExtensions,
                        SyntaxFactory.IdentifierName("AddControllers")))
                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("services")));

            var declareBuilder = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(mvcBuilder)
                    .AddVariables(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("builder"))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(getBuilder))));

            method = method.AddBodyStatements(declareBuilder);

            var populateFeature =
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("builder"),
                            SyntaxFactory.IdentifierName("PartManager")),
                        SyntaxFactory.IdentifierName("PopulateFeature")))
                    .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.ObjectCreationExpression(controllerFeature).AddArgumentListArguments()));

            method = method.AddBodyStatements(SyntaxFactory.ExpressionStatement(populateFeature));

            var transient = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    serviceDescriptor,
                    SyntaxFactory.GenericName("Transient")
                        .AddTypeArgumentListArguments(
                            controllerActivator, serviceBasedControllerActivator)))
                .AddArgumentListArguments();
            
            var bindControllerActivator = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    serviceCollectionDescriptorExtensions,
                    SyntaxFactory.IdentifierName("Replace")))
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("builder"),
                            SyntaxFactory.IdentifierName("Services"))
                        ),
                    SyntaxFactory.Argument(transient)
                    );

            method = method.AddBodyStatements(SyntaxFactory.ExpressionStatement(bindControllerActivator));

            foreach (var (dependency, lifetime, _) in dependencies)
            {
                string lifetimeName = lifetime switch
                {
                    Lifetime.ContainerSingleton => "AddSingleton",
                    Lifetime.Scoped => "AddScoped",
                    _ => "AddTransient"
                };

                var curDependency = _typeResolver.Resolve(dependency, null, dependency.Type.Locations);

                var resolve = SyntaxFactory.CastExpression(dependency, curDependency.ObjectBuilder.Build(_buildStrategy, curDependency));

                var serviceProviderInstance = new SemanticType(semanticModel.Compilation.GetTypeByMetadataName(typeof(ServiceProviderInstance).ToString())!, semanticModel);

                var serviceProviderValue = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    serviceProviderInstance,
                    SyntaxFactory.IdentifierName(nameof(ServiceProviderInstance.ServiceProvider)));

                var tryBlock = SyntaxFactory.Block()
                    .AddStatements(SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression, serviceProviderValue, SyntaxFactory.IdentifierName("serviceProvider"))))
                    .AddStatements(SyntaxFactory.ReturnStatement(resolve));

                var finallyBlock = SyntaxFactory.Block()
                    .AddStatements(SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression, serviceProviderValue, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));

                var tryStatement = SyntaxFactory.TryStatement(
                    tryBlock,
                    SyntaxFactory.List<CatchClauseSyntax>(),
                    SyntaxFactory.FinallyClause(finallyBlock));

                var resolveLambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier("serviceProvider")), SyntaxFactory.Block().AddStatements(tryStatement));

                var bind= 
                    SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                serviceCollectionServiceExtensions,
                                SyntaxFactory.IdentifierName(lifetimeName)))
                        .AddArgumentListArguments(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("services")),
                            SyntaxFactory.Argument(resolveLambda)
                        );

                method = method.AddBodyStatements(SyntaxFactory.ExpressionStatement(bind));
            }

            method = method.AddBodyStatements(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("services")));
            yield return method;
        }
    }
}
