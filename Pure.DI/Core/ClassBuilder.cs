// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable IdentifierTypo
namespace Pure.DI.Core;

using NS35EBD81B;

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
    private readonly IMembersBuilder[] _membersBuilder;
    private readonly IDiagnostic _diagnostic;
    private readonly IBindingsProbe _bindingsProbe;
    private readonly IArgumentsSupport _argumentsSupport;
    private readonly IBuildContext _buildContext;
    private readonly IMemberNameService _memberNameService;
    private readonly ResolverMetadata _metadata;

    public ClassBuilder(
        IBuildContext buildContext,
        IMemberNameService memberNameService,
        ResolverMetadata metadata,
        IEnumerable<IMembersBuilder> membersBuilder,
        IDiagnostic diagnostic,
        IBindingsProbe bindingsProbe,
        IArgumentsSupport argumentsSupport,
        ISettings settings)
    {
        _buildContext = buildContext;
        _memberNameService = memberNameService;
        _metadata = metadata;
        _membersBuilder = membersBuilder.OrderBy(i => i.Order).ToArray();
        _diagnostic = diagnostic;
        _bindingsProbe = bindingsProbe;
        _argumentsSupport = argumentsSupport;
    }

    public CompilationUnitSyntax Build(SemanticModel semanticModel)
    {
        var classModifiers = new List<SyntaxToken>();
        _buildContext.NameService.ReserveName(_metadata.ComposerTypeName);
        if (_metadata.Owner == null)
        {
            classModifiers.Add(SyntaxKind.InternalKeyword.WithSpace());
            classModifiers.Add(SyntaxKind.StaticKeyword.WithSpace());
            classModifiers.Add(SyntaxKind.PartialKeyword.WithSpace());
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
        
        var registerDisposableTypeSyntax = SyntaxFactory.ParseTypeName(typeof(RegisterDisposable).FullName.ReplaceNamespace());
        var registerDisposableEventTypeSyntax = SyntaxFactory.ParseTypeName(typeof(RegisterDisposableEvent).FullName.ReplaceNamespace());
        var iContextTypeSyntax = SyntaxFactory.ParseTypeName(typeof(IContext).FullName.ReplaceNamespace());
        var contextTypeSyntax = SyntaxFactory.ParseTypeName(_memberNameService.GetName(MemberNameKind.ContextClass));
        var members = _membersBuilder.SelectMany(i => i.BuildMembers(semanticModel)).Select(i => i.WithNewLine().WithNewLine()).ToArray();
        var resolverClass = SyntaxRepo.ClassDeclaration(_metadata.ComposerTypeName)
            .WithKeyword(SyntaxKind.ClassKeyword.WithSpace())
            .AddModifiers(classModifiers.ToArray())
            .AddMembers(
                SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(contextTypeSyntax)
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator(_memberNameService.GetName(MemberNameKind.ContextField))
                                    .WithSpace()
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxRepo.ObjectCreationExpression(contextTypeSyntax).AddArgumentListArguments()))))
                    .AddModifiers(
                        SyntaxKind.PrivateKeyword.WithSpace(),
                        SyntaxKind.StaticKeyword.WithSpace(),
                        SyntaxKind.ReadOnlyKeyword.WithSpace())
                    .WithCommentBefore("// Shared context to use in factories")
                    .WithNewLine()
                    .WithNewLine())
            .AddMembers(members)
            .AddMembers(SyntaxRepo.FinalDisposeMethodSyntax.AddBodyStatements(_buildContext.FinalDisposeStatements.ToArray()).WithNewLine())
            .AddMembers(
                SyntaxRepo.EventFieldDeclaration(
                        SyntaxFactory.VariableDeclaration(registerDisposableTypeSyntax).AddVariables(
                            SyntaxFactory.VariableDeclarator(SyntaxRepo.OnDisposableEventName)
                                .WithSpace()
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.AnonymousMethodExpression()
                                            .WithDelegateKeyword(SyntaxKind.DelegateKeyword.WithSpace())
                                            .WithBlock(SyntaxFactory.Block())))))
                    .AddModifiers(
                        SyntaxKind.InternalKeyword.WithSpace(),
                        SyntaxKind.StaticKeyword.WithSpace())
                    .WithNewLine()
                    .WithNewLine())
            .AddMembers(
                SyntaxRepo.RaiseOnDisposableMethodSyntax.AddBodyStatements(
                    SyntaxRepo.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(SyntaxRepo.OnDisposableEventName))
                            .AddArgumentListArguments(
                                SyntaxFactory.Argument(
                                    SyntaxRepo.ObjectCreationExpression(registerDisposableEventTypeSyntax)
                                        .AddArgumentListArguments(
                                            SyntaxFactory.Argument(SyntaxFactory.CastExpression(SyntaxRepo.DisposableTypeSyntax, SyntaxFactory.IdentifierName("disposable"))),
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("lifetime")))))),
                    SyntaxRepo.ReturnStatement(SyntaxFactory.IdentifierName("disposable"))
                ).WithNewLine().WithNewLine())
            .AddMembers(
                SyntaxRepo.ClassDeclaration(_memberNameService.GetName(MemberNameKind.ContextClass))
                    .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace())
                    .AddModifiers(SyntaxKind.SealedKeyword.WithSpace())
                    .AddBaseListTypes(SyntaxFactory.SimpleBaseType(iContextTypeSyntax))
                    .AddMembers(
                        SyntaxRepo.CreateTResolveMethodSyntax(SyntaxKind.PublicKeyword)
                            .AddBodyStatements(
                                SyntaxRepo.ReturnStatement()
                                    .WithExpression(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ParseName(_metadata.ComposerTypeName),
                                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                                SyntaxFactory.GenericName(nameof(IContext.Resolve))
                                                    .AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax)))
                                            .AddArgumentListArguments(_argumentsSupport.GetArguments().ToArray()))))
                    .AddMembers(
                        SyntaxRepo.CreateTResolveMethodSyntax(SyntaxKind.PublicKeyword)
                            .AddParameterListParameters(SyntaxRepo.Parameter(SyntaxFactory.Identifier("tag")).WithType(SyntaxRepo.ObjectTypeSyntax))
                            .AddBodyStatements(
                                SyntaxRepo.ReturnStatement()
                                    .WithExpression(
                                        SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.ParseName(_metadata.ComposerTypeName),
                                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                                    SyntaxFactory.GenericName(nameof(IContext.Resolve))
                                                        .AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax)))
                                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")))
                                            .AddArgumentListArguments(_argumentsSupport.GetArguments().ToArray()))))
                    .WithNewLine())
            .WithNewLine()
            .WithPragmaWarningDisable(0067, 8600, 8602, 8603, 8604, 8618, 8625);

        var rootNode = CreateRootNode(_metadata.SetupNode, new []{SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(Defaults.DefaultNamespace).WithSpace())}, resolverClass);
        var sampleDependency = _metadata.Bindings.LastOrDefault()?.Dependencies.FirstOrDefault()?.ToString() ?? "T";
        _diagnostic.Information(Diagnostics.Information.Generated, $"{_metadata.ComposerTypeName} was generated. Please use a method like {_metadata.ComposerTypeName}.Resolve<{sampleDependency}>() to create a composition root.", _metadata.Bindings.Where(i => i.Location != default).Select(i => i.Location!).ToArray());
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