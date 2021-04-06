namespace Pure.DI.Core
{
    using System.Collections.Generic;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class NameService : INameService
    {
        private readonly Dictionary<MemberKey, string> _names = new();
        private readonly Dictionary<string, int> _ids = new();

        public string FindName(MemberKey memberKey)
        {
            if (_names.TryGetValue(memberKey, out var name))
            {
                return name;
            }

            var newName = memberKey.Prefix;
            if (!_ids.TryGetValue(newName, out var id))
            {
                _ids.Add(newName, 0);
            }
            else
            {
                _ids[newName] = id + 1;
                newName = newName + id;
            }

            _names.Add(memberKey, newName);
            return newName;
        }

        public void Reset()
        {
            _ids.Clear();
            _names.Clear();
        }
    }
}
