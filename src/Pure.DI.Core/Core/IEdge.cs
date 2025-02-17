// ReSharper disable UnusedMemberInSuper.Global

namespace Pure.DI.Core;

interface IEdge<out TVertex>
{
    TVertex Source { get; }

    TVertex Target { get; }
}