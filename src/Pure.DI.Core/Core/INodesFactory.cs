namespace Pure.DI.Core;

interface INodesFactory
{
    IEnumerable<DependencyNode> CreateNodes(MdSetup setup, ITypeConstructor typeConstructor, MdBinding binding);
}