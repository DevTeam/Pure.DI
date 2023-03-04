namespace Pure.DI.Core;

internal class FactoryNamesRewriter: CSharpSyntaxRewriter
{
    private readonly IReadOnlyDictionary<string, string> _namesMap;

    public FactoryNamesRewriter(IReadOnlyDictionary<string, string> namesMap) => _namesMap = namesMap;

    public override SyntaxNode? VisitBlock(BlockSyntax block)
    {
        // Removes declarations of injected local variables
        var statements = new List<StatementSyntax>();
        foreach (var statement in block.Statements)
        {
            if (statement is LocalDeclarationStatementSyntax localDeclarationStatement)
            {
                var variables = localDeclarationStatement.Declaration.Variables
                    .Where(i => !_namesMap.ContainsKey(i.Identifier.Text))
                    .ToArray();
                
                if (!variables.Any())
                {
                    continue;
                }

                var newStatement = localDeclarationStatement.WithDeclaration(
                    localDeclarationStatement.Declaration
                        .WithVariables(SyntaxFactory.SeparatedList(variables)));
                
                statements.Add(newStatement);
            }
            else
            {
                statements.Add(statement);   
            }
        }

        block = SyntaxFactory.Block(statements);
        return base.VisitBlock(block);
    }
    
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax identifierName) => 
        _namesMap.TryGetValue(identifierName.Identifier.Text, out var newName)
            // Renames declarations of injected local variables
            ? SyntaxFactory.IdentifierName(newName)
            : base.VisitIdentifierName(identifierName);
}