namespace Pure.DI.Core;

using System.Collections.Concurrent;

class OverridesRegistry : IOverridesRegistry
{
    private readonly ConcurrentDictionary<Root, List<DpOverride>> _overrides = new();

    public void Register(Root root, DpOverride @override) =>
        _overrides.AddOrUpdate(root, _ => [@override], (_, list) => {
            list.Add(@override);
            return list;
        });

    public IEnumerable<DpOverride> GetOverrides(Root root) =>
        _overrides.TryGetValue(root, out var overrides)
            ? overrides.GroupBy(i => i.Source.Id).Select(i => i.First()).ToList()
            : [];
}