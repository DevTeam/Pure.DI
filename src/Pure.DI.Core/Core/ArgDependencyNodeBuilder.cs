// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class ArgDependencyNodeBuilder(ILocationProvider locationProvider)
    : IBuilder<DependencyNodeBuildContext, IEnumerable<DependencyNode>>
{
    public IEnumerable<DependencyNode> Build(DependencyNodeBuildContext ctx)
    {
        foreach (var binding in ctx.Setup.Bindings)
        {
            if (binding.Arg is not {} arg)
            {
                continue;
            }

            yield return new DependencyNode(0, binding, ctx.TypeConstructor, Arg: new DpArg(arg, binding, locationProvider));
        }
    }
}