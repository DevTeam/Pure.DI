namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IBindingStatementsStrategy
    {
        IEnumerable<StatementSyntax> CreateStatements(BindingMetadata binding, ITypeSymbol contractType);
    }
}