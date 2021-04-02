namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class NameService : INameService
    {
        private readonly Dictionary<Key, string> _names = new();
        private readonly Dictionary<string, int> _ids = new();

        public string FindName(string prefix, INamedTypeSymbol contractType, ExpressionSyntax? tag)
        {
            var key = new Key(prefix, contractType, tag);
            if (_names.TryGetValue(key, out var name))
            {
                return name;
            }

            var newName = prefix;
            if (!_ids.TryGetValue(newName, out var id))
            {
                _ids.Add(newName, 0);
            }
            else
            {
                _ids[newName] = id + 1;
                newName = newName + id;
            }

            _names.Add(key, newName);
            return newName;
        }

        private readonly struct Key
        {
            private readonly INamedTypeSymbol _type;
            private readonly ExpressionSyntax? _tag;
            private readonly string _prefix;

            public Key(string prefix, INamedTypeSymbol contractType, ExpressionSyntax? tag)
            {
                _type = contractType;
                _tag = tag;
                _prefix = prefix;
            }

            private bool Equals(Key other) =>
                _type.Equals(other._type, SymbolEqualityComparer.Default) && _tag?.ToString() == other._tag?.ToString() && _prefix == other._prefix;

            public override bool Equals(object obj) =>
                obj is Key other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _type.GetHashCode();
                    hashCode = (hashCode * 397) ^ (_tag != null ? _tag.ToString().GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ _prefix.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}
