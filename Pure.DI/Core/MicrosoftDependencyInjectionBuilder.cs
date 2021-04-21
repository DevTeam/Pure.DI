namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class MicrosoftDependencyInjectionBuilder: IMembersBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IBuildContext _buildContext;

        public MicrosoftDependencyInjectionBuilder(ResolverMetadata metadata, IBuildContext buildContext)
        {
            _metadata = metadata;
            _buildContext = buildContext;
        }

        public int Order => 1;

        public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
        {
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

            var dependencies =
                    from binding in _metadata.Bindings.Concat(_buildContext.AdditionalBindings)
                    where !binding.Tags.Any()
                    from dependency in binding.Dependencies
                    where dependency.Type.IsReferenceType
                    group binding.Lifetime by dependency;

            foreach (var dependency in dependencies)
            {
                var lifetime = dependency.Last();
                string lifetimeName;
                switch (lifetime)
                {
                    case Lifetime.ContainerSingleton:
                        lifetimeName = "AddSingleton";
                        break;

                    case Lifetime.Scoped:
                        lifetimeName = "AddScoped";
                        break;

                    default:
                        lifetimeName = "AddTransient";
                        break;
                }


                var resolve = 
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(_metadata.TargetTypeName),
                            SyntaxFactory.GenericName("Resolve").AddTypeArgumentListArguments(dependency.Key)));

                var resolveLambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier("i")), resolve);

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
