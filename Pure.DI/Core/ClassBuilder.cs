// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable IdentifierTypo
namespace Pure.DI.Core;

#if ROSLYN38
    using NamespaceType = Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax;
#else
using NamespaceType = BaseNamespaceDeclarationSyntax;
#endif

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
// ReSharper disable once ClassNeverInstantiated.Global
internal class ClassBuilder : IClassBuilder
{
    private static readonly UsingDirectiveSyntax[] AdditionalUsings =
    {
        SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Pure.DI"))
    };
    
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
        _buildContext.NameService.ReserveName(_metadata.ComposerTypeName);
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

        _bindingsProbe.Probe();
        var contextTypeSyntax = SyntaxFactory.ParseTypeName(_memberNameService.GetName(MemberNameKind.ContextClass));
        var resolverClass = SyntaxFactory.ClassDeclaration(_metadata.ComposerTypeName)
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
            .AddMembers(SyntaxRepo.FinalDisposeMethodSyntax.AddBodyStatements(_buildContext.FinalDisposeStatements.ToArray()))
            .AddMembers(
                SyntaxFactory.EventFieldDeclaration(
                        SyntaxFactory.VariableDeclaration(SyntaxRepo.RegisterDisposableTypeSyntax).AddVariables(
                            SyntaxFactory.VariableDeclarator(SyntaxRepo.OnDisposableEventName)
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.AnonymousMethodExpression()
                                            .WithDelegateKeyword(SyntaxFactory.Token(SyntaxKind.DelegateKeyword))
                                            .WithBlock(SyntaxFactory.Block())))))
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .AddMembers(
                SyntaxRepo.RaiseOnDisposableMethodSyntax.AddBodyStatements(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(SyntaxRepo.OnDisposableEventName))
                            .AddArgumentListArguments(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.ObjectCreationExpression(SyntaxRepo.RegisterDisposableEventTypeSyntax)
                                        .AddArgumentListArguments(
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("disposable")),
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("lifetime")))))),
                    SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("disposable"))
                ))
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
                                                SyntaxFactory.ParseName(_metadata.ComposerTypeName),
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
                                                    SyntaxFactory.ParseName(_metadata.ComposerTypeName),
                                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                                    SyntaxFactory.GenericName(nameof(IContext.Resolve))
                                                        .AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax)))
                                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag"))))))
            ).WithPragmaWarningDisable(0067, 8600, 8602, 8603, 8604, 8618, 8625);

        var rootNode = CreateRootNode(_metadata.SetupNode, AdditionalUsings, resolverClass);
        var sampleDependency = _metadata.Bindings.LastOrDefault()?.Dependencies.FirstOrDefault()?.ToString() ?? "T";
        _diagnostic.Information(Diagnostics.Information.Generated, $"{_metadata.ComposerTypeName} was generated. Please use a method like {_metadata.ComposerTypeName}.Resolve<{sampleDependency}>() to create a composition root.", _metadata.Bindings.FirstOrDefault()?.Location);
        return rootNode;
    }

    [Pure]
    private static CompilationUnitSyntax CreateRootNode(SyntaxNode targetNode, UsingDirectiveSyntax[] additionalUsings, params MemberDeclarationSyntax[] members)
    {
        var namespaces = targetNode.Ancestors().OfType<NamespaceType>();
        NamespaceType? rootNamespace = default;
        foreach (var ns in namespaces)
        {
            var nextNs = ns.WithMembers(new SyntaxList<MemberDeclarationSyntax>(Enumerable.Empty<MemberDeclarationSyntax>()));
            rootNamespace = rootNamespace == default
                ? nextNs.AddMembers(members).AddUsings(GetUsings(nextNs.Usings, additionalUsings))
                : nextNs.AddMembers(rootNamespace);
        }

        var baseCompilationUnit = targetNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
        var rootCompilationUnit = (baseCompilationUnit ?? SyntaxFactory.CompilationUnit())
            .WithMembers(new SyntaxList<MemberDeclarationSyntax>(Enumerable.Empty<MemberDeclarationSyntax>()));

        return rootNamespace != default
            ? rootCompilationUnit.AddMembers(rootNamespace)
            : rootCompilationUnit.AddUsings(GetUsings(rootCompilationUnit.Usings, additionalUsings)).AddMembers(members);
    }

    [Pure]
    private static UsingDirectiveSyntax[] GetUsings(IEnumerable<UsingDirectiveSyntax> usings, IEnumerable<UsingDirectiveSyntax> additionalUsings)
    {
        var currentUsins = usings.Select(i => i.Name.ToString()).ToImmutableHashSet();
        return additionalUsings.Where(i => !currentUsins.Contains(i.Name.ToString())).ToArray();
    }
}