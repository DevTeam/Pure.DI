namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class NameService : INameService
{
    private readonly Dictionary<MemberKey, string> _names = new();
    private readonly Dictionary<string, int> _ids = new();
    private readonly HashSet<string> _reserved = new(StringComparer.CurrentCultureIgnoreCase);

    public string FindName(MemberKey memberKey)
    {
        if (_names.TryGetValue(memberKey, out var name))
        {
            return name;
        }

        string newName;
        do
        {
            if (!_ids.TryGetValue(memberKey.Prefix, out var id))
            {
                _ids.Add(memberKey.Prefix, 0);
            }
            else
            {
                _ids[memberKey.Prefix] = id + 1;
            }

            newName = memberKey.Prefix + (id == 0 ? "" : id.ToString());
        } while (_reserved.Contains(newName));

        _names.Add(memberKey, newName);
        _reserved.Add(newName);
        return newName;
    }

    public void ReserveName(string name) => _reserved.Add(name);
}