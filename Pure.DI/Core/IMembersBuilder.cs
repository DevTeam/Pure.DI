namespace Pure.DI.Core;

internal interface IMembersBuilder
{
    int Order { get; }

    IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel);
}