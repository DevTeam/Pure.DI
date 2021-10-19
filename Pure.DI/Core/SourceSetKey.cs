namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    internal class SourceSetKey
    {
        private readonly LanguageVersion _languageVersion;
        private readonly string _ns;

        public SourceSetKey(LanguageVersion languageVersion, string ns)
        {
            _languageVersion = languageVersion;
            _ns = ns;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            SourceSetKey other = (SourceSetKey)obj;
            return _languageVersion == other._languageVersion && _ns == other._ns;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)_languageVersion * 397) ^ _ns.GetHashCode();
            }
        }
    }
}