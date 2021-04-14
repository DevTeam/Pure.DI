namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal readonly struct MemberKey
    {
        private readonly SemanticType _implementation;
        private readonly ExpressionSyntax? _tag;
        public readonly string Prefix;

        public MemberKey(string prefix, SemanticType implementation, ExpressionSyntax? tag)
        {
            _implementation = implementation;
            _tag = tag;
            Prefix = prefix;
        }

        private bool Equals(MemberKey other) =>
            _implementation.Equals(other._implementation) && _tag?.ToString() == other._tag?.ToString() && Prefix == other.Prefix;

        public override bool Equals(object obj) =>
            obj is MemberKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _implementation.GetHashCode();
                hashCode = (hashCode * 397) ^ (_tag != null ? _tag.ToString().GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Prefix.GetHashCode();
                return hashCode;
            }
        }
    }
}