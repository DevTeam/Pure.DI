// ReSharper disable MemberCanBePrivate.Global
namespace Pure.DI.Core
{
    using System.Linq;

    internal readonly struct MemberKey
    {
        public readonly string Prefix;
        private readonly object _id;

        public MemberKey(string prefix, Dependency dependency)
        {
            Prefix = new string(prefix.Where(i => char.IsLetterOrDigit(i) || i == '_').ToArray());
            _id = dependency.Binding.Id;
        }

        public bool Equals(MemberKey other) => Prefix == other.Prefix && _id.Equals(other._id);

        public override bool Equals(object? obj) => obj is MemberKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Prefix.GetHashCode() * 397) ^ _id.GetHashCode();
            }
        }
    }
}