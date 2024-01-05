namespace Pure.DI.Core;

internal sealed class ArgDependencyNodeBuilder : IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(MdSetup setup)
    {
        foreach (var binding in setup.Bindings)
        {
            if (binding.Arg is not {} arg)
            {
                continue;
            }

            yield return new DependencyNode(0, binding, Arg: new DpArg(arg, binding));
        }
    }
}