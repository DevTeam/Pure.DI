namespace Pure.DI.Core;

using System.Collections.Concurrent;

class OverridesRegistry : IOverridesRegistry
{
    private readonly ConcurrentDictionary<int, List<DpOverride>> _overrides = new();

    public void Register(DependencyNode rootNode, DpOverride @override) =>
        _overrides.AddOrUpdate(rootNode.Binding.Id, _ => [@override], (_, list) => {
            list.Add(@override);
            return list;
        });

    public IEnumerable<DpOverride> GetOverrides(DependencyNode rootNode) =>
        _overrides.TryGetValue(rootNode.Binding.Id, out var overrides)
            ? overrides.GroupBy(i => i.Source.Id).Select(i => i.First()).ToList()
            : [];
}