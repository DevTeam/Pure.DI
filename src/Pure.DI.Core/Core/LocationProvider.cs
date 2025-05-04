namespace Pure.DI.Core;

class LocationProvider : ILocationProvider
{
    public Location GetLocation(SyntaxNode node)
    {
        return node.GetLocation();
    }

    public Location GetLocation(SyntaxToken token)
    {
        return token.GetLocation();
    }
}