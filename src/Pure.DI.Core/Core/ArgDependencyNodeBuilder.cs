namespace Pure.DI.Core;

internal class ArgDependencyNodeBuilder : IBuilder<MdSetup, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(MdSetup setup, CancellationToken cancellationToken)
    {
        foreach (var binding in setup.Bindings)
        {
            if (binding.Arg is not {} arg)
            {
                continue;
            }

            yield return new DependencyNode(Arg: new DpArg(arg, binding));
        }
    }
}