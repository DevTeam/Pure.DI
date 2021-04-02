namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class NameService : INameService
    {
        private readonly Dictionary<Key, string> _names = new Dictionary<Key, string>();
        private readonly Dictionary<string, int> _ids = new Dictionary<string, int>();

        public string FindName(string prefix, INamedTypeSymbol contractType, ExpressionSyntax tag)
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
            public readonly INamedTypeSymbol Type;
            public readonly ExpressionSyntax Tag;
            public readonly string Prefix;

            public Key(string prefix, INamedTypeSymbol contractType, ExpressionSyntax tag)
            {
                Type = contractType;
                Tag = tag;
                Prefix = prefix;
            }

            public bool Equals(Key other)
            {
                return Type.Equals(other.Type, SymbolEqualityComparer.IncludeNullability) && Tag?.ToString() == other.Tag?.ToString() && Prefix == other.Prefix;
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Type != null ? Type.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Tag != null ? Tag.ToString().GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Prefix != null ? Prefix.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}
