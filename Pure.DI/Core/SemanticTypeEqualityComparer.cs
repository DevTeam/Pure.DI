namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal class SemanticTypeEqualityComparer: IEqualityComparer<SemanticType>
    {
        public static readonly IEqualityComparer<SemanticType> Default = new SemanticTypeEqualityComparer();

        public bool Equals(SemanticType x, SemanticType y) =>
            SymbolEqualityComparer.Default.Equals(x.Type, y.Type);

        public int GetHashCode(SemanticType obj) =>
            SymbolEqualityComparer.Default.GetHashCode(obj.Type);
    }
}
