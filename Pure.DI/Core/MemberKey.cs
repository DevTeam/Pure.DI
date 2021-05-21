// ReSharper disable MemberCanBePrivate.Global
namespace Pure.DI.Core
{
    using System.Linq;

    internal readonly struct MemberKey
    {
        public readonly string Prefix;
        private readonly object _bindingId;
        
        public MemberKey(string prefix, Dependency dependency) 
            : this(prefix, dependency.Binding.Id)
        {
        }

        public MemberKey(string prefix, object bindingId)
        {
            Prefix = new string(prefix.Where(i => char.IsLetterOrDigit(i) || i == '_').ToArray());
            _bindingId = bindingId;
        }

        public bool Equals(MemberKey other) => Prefix == other.Prefix && _bindingId.Equals(other._bindingId);

        public override bool Equals(object? obj) => obj is MemberKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Prefix.GetHashCode() * 397) ^ _bindingId.GetHashCode();
            }
        }
    }
}