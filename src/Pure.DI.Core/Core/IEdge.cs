namespace Pure.DI.Core;

internal interface IEdge<out TVertex>
{
    TVertex Source { get; }

    TVertex Target { get; }
}