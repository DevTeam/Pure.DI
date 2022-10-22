// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal sealed class ContextClassMembersBuilder: IMembersBuilder
{
    private readonly IMemberNameService _memberNameService;
    private readonly ResolverMetadata _metadata;
    private readonly IArgumentsSupport _argumentsSupport;

    public ContextClassMembersBuilder(
        IMemberNameService memberNameService,
        ResolverMetadata metadata,
        IArgumentsSupport argumentsSupport)
    {
        _memberNameService = memberNameService;
        _metadata = metadata;
        _argumentsSupport = argumentsSupport;
    }

    public int Order => 0;
    
    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
    {
        var contextTypeSyntax = SyntaxFactory.ParseTypeName(typeof(IContext).FullName.ReplaceNamespace());
        yield return SyntaxRepo.ClassDeclaration(_memberNameService.GetName(MemberNameKind.ContextClass))
            .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace())
            .AddModifiers(SyntaxKind.SealedKeyword.WithSpace())
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(contextTypeSyntax))
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
            .WithNewLine();
    }
}