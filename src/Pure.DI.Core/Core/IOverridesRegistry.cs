namespace Pure.DI.Core;

interface IOverridesRegistry
{
    void Register(Root root, DpOverride @override);

    IEnumerable<DpOverride> GetOverrides(Root root);
}