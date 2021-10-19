namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    internal class SourceSetKey
    {
        private readonly LanguageVersion _languageVersion;

        public SourceSetKey(LanguageVersion languageVersion) =>
            _languageVersion = languageVersion;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            SourceSetKey other = (SourceSetKey)obj;
            return _languageVersion == other._languageVersion;
        }

        public override int GetHashCode() => (int)_languageVersion;
    }
}