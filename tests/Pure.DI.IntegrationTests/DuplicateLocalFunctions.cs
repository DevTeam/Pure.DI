namespace Pure.DI.IntegrationTests;

using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Finds the local functions that the generated code of a member repeats although one of them would do.
/// A local function that reads no variable of the code around it, such as a value overridden by a lambda or an accumulator,
/// can be declared once at the level of its member, so a copy of it in another lambda only makes the code larger.
/// Such copies grow with the number of dependency paths, see #184 and #186.
/// </summary>
static class DuplicateLocalFunctions
{
    public static IReadOnlyList<string> Find(Compilation compilation, IEnumerable<SyntaxTree> syntaxTrees)
    {
        var duplicates = new List<string>();
        foreach (var syntaxTree in syntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var functionsByMember = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<LocalFunctionStatementSyntax>()
                .Where(function => !ReadsOuterVariables(semanticModel, function))
                .GroupBy(function => function.FirstAncestorOrSelf<MemberDeclarationSyntax>());

            foreach (var memberFunctions in functionsByMember)
            {
                duplicates.AddRange(
                    from sameFunctions in memberFunctions.GroupBy(Normalize)
                    where sameFunctions.Count() > 1
                    select $"{GetMemberName(memberFunctions.Key)}: {string.Join(", ", sameFunctions.Select(i => i.Identifier.Text))}");
            }
        }

        return duplicates;
    }

    private static bool ReadsOuterVariables(SemanticModel semanticModel, LocalFunctionStatementSyntax function)
    {
        SyntaxNode? body = (SyntaxNode?)function.Body ?? function.ExpressionBody?.Expression;
        if (body is null)
        {
            return false;
        }

        var dataFlow = body is ExpressionSyntax expression
            ? semanticModel.AnalyzeDataFlow(expression)
            : semanticModel.AnalyzeDataFlow((StatementSyntax)body);
        if (dataFlow is not { Succeeded: true })
        {
            // The function is kept out of the comparison when its variables are not known.
            return true;
        }

        return dataFlow.ReadInside.Concat(dataFlow.WrittenInside).Any(symbol =>
            symbol is ILocalSymbol or IParameterSymbol { IsThis: false }
            && symbol.DeclaringSyntaxReferences.Any(reference =>
                reference.SyntaxTree == function.SyntaxTree
                && !function.Span.Contains(reference.Span)));
    }

    // The code of a function with the names that the function declares itself replaced by their order,
    // because the copies of a function declare the same variables under different unique names.
    private static string Normalize(LocalFunctionStatementSyntax function)
    {
        var declaredNames = new Dictionary<string, string>();
        foreach (var identifier in GetDeclaredIdentifiers(function))
        {
            if (!declaredNames.ContainsKey(identifier.Text))
            {
                declaredNames.Add(identifier.Text, $"@{declaredNames.Count}");
            }
        }

        var code = new StringBuilder();
        foreach (var token in function.DescendantTokens())
        {
            code.Append(token.IsKind(SyntaxKind.IdentifierToken) && declaredNames.TryGetValue(token.Text, out var name) ? name : token.Text);
            code.Append(' ');
        }

        return code.ToString();
    }

    private static IEnumerable<SyntaxToken> GetDeclaredIdentifiers(LocalFunctionStatementSyntax function) =>
        function.DescendantNodesAndSelf().Select(node => node switch
        {
            LocalFunctionStatementSyntax localFunction => localFunction.Identifier,
            VariableDeclaratorSyntax variable => variable.Identifier,
            ParameterSyntax parameter => parameter.Identifier,
            SingleVariableDesignationSyntax designation => designation.Identifier,
            ForEachStatementSyntax forEach => forEach.Identifier,
            CatchDeclarationSyntax @catch => @catch.Identifier,
            _ => default
        }).Where(identifier => identifier.IsKind(SyntaxKind.IdentifierToken));

    private static string GetMemberName(MemberDeclarationSyntax? member) =>
        member switch
        {
            PropertyDeclarationSyntax property => property.Identifier.Text,
            MethodDeclarationSyntax method => method.Identifier.Text,
            ConstructorDeclarationSyntax constructor => constructor.Identifier.Text,
            _ => member?.Kind().ToString() ?? "?"
        };
}
