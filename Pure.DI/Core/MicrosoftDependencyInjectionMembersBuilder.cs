namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal class MicrosoftDependencyInjectionMembersBuilder : IMembersBuilder
{
    private readonly ResolverMetadata _metadata;
    private readonly IBuildContext _buildContext;
    private readonly ITypeResolver _typeResolver;
    private readonly IBuildStrategy _buildStrategy;
    private readonly IDiagnostic _diagnostic;
    private readonly Log<MicrosoftDependencyInjectionMembersBuilder> _log;
    private readonly ITracer _tracer;
    private readonly IStatementsBlockWrapper _statementsBlockWrapper;
    private readonly IArgumentsSupport _argumentsSupport;
    private readonly IStatementsBlockWrapper[] _statementsBlockWrappers;

    public MicrosoftDependencyInjectionMembersBuilder(
        ResolverMetadata metadata,
        IBuildContext buildContext,
        ITypeResolver typeResolver,
        IBuildStrategy buildStrategy,
        IDiagnostic diagnostic,
        Log<MicrosoftDependencyInjectionMembersBuilder> log,
        ITracer tracer,
        IStatementsBlockWrapper statementsBlockWrapper,
        IArgumentsSupport argumentsSupport,
        IStatementsBlockWrapper[] statementsBlockWrappers)
    {
        _metadata = metadata;
        _buildContext = buildContext;
        _typeResolver = typeResolver;
        _buildStrategy = buildStrategy;
        _diagnostic = diagnostic;
        _log = log;
        _tracer = tracer;
        _statementsBlockWrapper = statementsBlockWrapper;
        _argumentsSupport = argumentsSupport;
        _statementsBlockWrappers = statementsBlockWrappers;
    }

    public int Order => 1;

    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
    {
        _log.Trace(() => new[]
        {
            "Start"
        });

        var dependencies = (
            from binding in _metadata.Bindings.Concat(_buildContext.AdditionalBindings)
            from dependency in binding.Dependencies
            let tags = binding.GetTags(dependency)
            where !tags.Any()
            where dependency.Type.IsReferenceType && !dependency.IsComposedGenericTypeMarker
            group binding by dependency
            into groups
            let item = groups.First()
            select (resolvedDependency: groups.Key, lifetime: item.Lifetime, item)).ToArray();

        var serviceProviderType = semanticModel.Compilation.GetTypeByMetadataName(typeof(IServiceProvider).FullName.ReplaceNamespace());
        var serviceCollectionType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
        var serviceDescriptorType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceDescriptor");
        var serviceCollectionServiceExtensionsType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions");

        _log.Trace(() => new[]
        {
            $"serviceProviderType: {serviceProviderType}", $"serviceCollectionType: {serviceCollectionType}", $"serviceDescriptorType: {serviceDescriptorType}", $"serviceCollectionServiceExtensionsType: {serviceCollectionServiceExtensionsType}"
        });

        if (
            serviceProviderType != null
            && serviceCollectionType != null
            && serviceDescriptorType != null
            && serviceCollectionServiceExtensionsType != null)
        {
            var extensionMethodName = $"Add{_metadata.ComposerTypeName}";
            var extensionKey = new MemberKey(extensionMethodName, GetType());
            yield return _buildContext.GetOrAddMember(extensionKey, () =>
            {
                var serviceProvider = new SemanticType(serviceProviderType, semanticModel);
                var serviceCollection = new SemanticType(serviceCollectionType, semanticModel);
                var serviceDescriptor = new SemanticType(serviceDescriptorType, semanticModel);
                var serviceCollectionServiceExtensions = new SemanticType(serviceCollectionServiceExtensionsType, semanticModel);

                var thisParameter = SyntaxFactory.Parameter(
                    SyntaxFactory.List<AttributeListSyntax>(),
                    SyntaxFactory.TokenList(SyntaxKind.ThisKeyword.WithSpace()),
                    serviceCollection,
                    SyntaxFactory.Identifier("services").WithSpace(),
                    null);

                var method = SyntaxRepo.MethodDeclaration(serviceCollection, $"Add{_metadata.ComposerTypeName}")
                    .AddModifiers(SyntaxKind.InternalKeyword.WithSpace(), SyntaxKind.StaticKeyword.WithSpace())
                    .AddParameterListParameters(thisParameter)
                    .AddParameterListParameters(_argumentsSupport.GetParameters().ToArray());

                var mvcBuilderType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IMvcBuilder");
                var mvcServiceCollectionExtensionsType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.MvcServiceCollectionExtensions");
                var controllerFeatureType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controllers.ControllerFeature");
                var controllerActivatorType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator");
                var serviceBasedControllerActivatorType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controllers.ServiceBasedControllerActivator");
                var serviceCollectionDescriptorExtensionsType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions");

                _log.Trace(() => new[]
                {
                    $"mvcBuilderType: {mvcBuilderType}", $"mvcServiceCollectionExtensionsType: {mvcServiceCollectionExtensionsType}", $"controllerFeatureType: {controllerFeatureType}", $"controllerActivatorType: {controllerActivatorType}", $"serviceBasedControllerActivatorType: {serviceBasedControllerActivatorType}", $"serviceCollectionDescriptorExtensionsType: {serviceCollectionDescriptorExtensionsType}"
                });

                var aspNetSupport = false;
                if (mvcBuilderType != null
                    && mvcServiceCollectionExtensionsType != null
                    && controllerFeatureType != null
                    && controllerActivatorType != null
                    && serviceBasedControllerActivatorType != null
                    && serviceCollectionDescriptorExtensionsType != null)
                {
                    aspNetSupport = true;
                    var mvcBuilder = new SemanticType(mvcBuilderType, semanticModel);
                    var mvcServiceCollectionExtensions = new SemanticType(mvcServiceCollectionExtensionsType, semanticModel);
                    var controllerFeature = new SemanticType(controllerFeatureType, semanticModel);
                    var controllerActivator = new SemanticType(controllerActivatorType, semanticModel);
                    var serviceBasedControllerActivator = new SemanticType(serviceBasedControllerActivatorType, semanticModel);
                    var serviceCollectionDescriptorExtensions = new SemanticType(serviceCollectionDescriptorExtensionsType, semanticModel);

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
                                    .WithSpace()
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
                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxRepo.ObjectCreationExpression(controllerFeature).AddArgumentListArguments()));

                    method = method.AddBodyStatements(SyntaxRepo.ExpressionStatement(populateFeature));

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

                    method = method.AddBodyStatements(SyntaxRepo.ExpressionStatement(bindControllerActivator));
                }
                else
                {
                    foreach (var (resoledDependency, lifetime, binding) in dependencies.Where(i => i.lifetime is Lifetime.Scoped or Lifetime.ContainerSingleton))
                    {
                        var error = $"Impossible to use the lifetime {lifetime} for {resoledDependency} outside an ASP.NET context.";
                        _diagnostic.Error(Diagnostics.Error.Unsupported, error, new []{ binding.Location }.Where(i => i != default).Select(i => i!).ToArray());
                        throw new HandledException(error);
                    }
                }

                foreach (var (resolvedDependency, lifetime, _) in dependencies)
                {
                    if (resolvedDependency.Equals(serviceProvider))
                    {
                        // Skip IServiceProvider
                        continue;
                    }

                    if (_buildContext.IsCancellationRequested)
                    {
                        _log.Trace(() => new[]
                        {
                            "Build canceled"
                        });

                        continue;
                    }

                    var dependency = _typeResolver.Resolve(resolvedDependency, null);
                    if (!dependency.IsResolved)
                    {
                        _tracer.Save();
                        continue;
                    }

                    string lifetimeName;
                    Optional<ExpressionSyntax> objectExpression;
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (lifetime)
                    {
                        case Lifetime.ContainerSingleton:
                            objectExpression = dependency.ObjectBuilder.TryBuild(_buildStrategy, dependency);
                            lifetimeName = "AddSingleton";
                            break;

                        case Lifetime.Scoped:
                            objectExpression = dependency.ObjectBuilder.TryBuild(_buildStrategy, dependency);
                            lifetimeName = "AddScoped";
                            break;

                        default:
                            objectExpression = _buildStrategy.TryBuild(dependency, resolvedDependency);
                            lifetimeName = "AddTransient";
                            break;
                    }

                    if (!objectExpression.HasValue)
                    {
                        _tracer.Save();
                        continue;
                    }

                    BlockSyntax lambdaBlock;
                    if (aspNetSupport)
                    {
                        var serviceProviderInstance = new SemanticType(semanticModel.Compilation.GetTypeByMetadataName(typeof(ServiceProviderInstance).FullName.ReplaceNamespace())!, semanticModel);
                        var serviceProviderValue = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            serviceProviderInstance,
                            SyntaxFactory.IdentifierName(nameof(ServiceProviderInstance.ServiceProvider)));
    
                        var tryBlock = SyntaxFactory.Block()
                            .AddStatements()
                            .AddStatements(SyntaxRepo.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression, serviceProviderValue, SyntaxFactory.IdentifierName("serviceProvider"))))
                            .AddStatements(SyntaxRepo.ReturnStatement(objectExpression.Value));
    
                        var finallyBlock = SyntaxFactory.Block()
                            .AddStatements(SyntaxRepo.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression, serviceProviderValue, SyntaxFactory.IdentifierName("prevServiceProvider"))));
    
                        lambdaBlock =
                            SyntaxFactory.Block()
                                .AddStatements(
                                    SyntaxFactory.LocalDeclarationStatement(
                                        SyntaxFactory.VariableDeclaration(serviceProvider)
                                            .AddVariables(
                                                SyntaxFactory.VariableDeclarator("prevServiceProvider")
                                                    .WithSpace()
                                                    .WithInitializer(SyntaxFactory.EqualsValueClause(serviceProviderValue))
                                            )
                                    )
                                )
                                .AddStatements(
                                    SyntaxFactory.TryStatement(
                                        tryBlock,
                                        SyntaxFactory.List<CatchClauseSyntax>(),
                                        SyntaxFactory.FinallyClause(finallyBlock)));
    
                        lambdaBlock = _statementsBlockWrapper.AddFinalizationStatements(lambdaBlock)!;
                    }
                    else
                    {
                        lambdaBlock = SyntaxFactory.Block()
                            .AddStatements(
                                SyntaxFactory.ReturnStatement(
                                    objectExpression.Value
                                        .WithSpace()));
                    }
                    
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var statementsFinalizer in _statementsBlockWrappers)
                    {
                        lambdaBlock = statementsFinalizer.AddFinalizationStatements(lambdaBlock)!;
                    }
                        
                    var resolveLambda = SyntaxFactory.SimpleLambdaExpression(SyntaxRepo.Parameter(SyntaxFactory.Identifier("serviceProvider")), lambdaBlock);

                    var bind =
                        SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    serviceCollectionServiceExtensions,
                                    SyntaxFactory.GenericName(lifetimeName).AddTypeArgumentListArguments(resolvedDependency.TypeSyntax)))
                            .AddArgumentListArguments(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("services")),
                                SyntaxFactory.Argument(resolveLambda)
                            );

                    method = method.AddBodyStatements(SyntaxRepo.ExpressionStatement(bind));
                }

                return method.AddBodyStatements(SyntaxRepo.ReturnStatement(SyntaxFactory.IdentifierName("services")));
            });
        }

        _log.Trace(() => new[]
        {
            "Finish"
        });
    }
}