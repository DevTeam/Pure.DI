namespace Pure.DI.Core;

interface ILocationProvider
{
    Location GetLocation(SyntaxNode node);

    Location GetLocation(SyntaxToken token);
}