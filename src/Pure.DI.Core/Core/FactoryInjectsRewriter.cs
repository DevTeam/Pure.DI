// ReSharper disable InvertIf
namespace Pure.DI.Core;

using System.Collections;

internal sealed class FactoryInjectsRewriter: CSharpSyntaxRewriter, IEnumerable<FactoryInjection>
{
    private readonly DpFactory _factory;
    private readonly List<FactoryInjection> _injections = new();
    private int _injectionId;
    private int _contextId;

    public FactoryInjectsRewriter(DpFactory factory)
    {
        _factory = factory;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<FactoryInjection> GetEnumerator() => _injections.GetEnumerator();

    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
    {
        _contextId++;
        return base.VisitSimpleLambdaExpression(node);
    }

    public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
    {
        _contextId++;
        return base.VisitParenthesizedLambdaExpression(node);
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count > 0 
            && invocation.Expression is MemberAccessExpressionSyntax
            {
                Name: GenericNameSyntax
                {
                    Identifier.Text: nameof(IContext.Inject),
                    TypeArgumentList.Arguments: [{ }]
                },
                Expression: IdentifierNameSyntax ctx
            }
            && ctx.Identifier.Text == _factory.Source.Context.Identifier.Text)
        {
            var injectionId = $"injection{_injectionId++}{Variable.Postfix}";
            switch (invocation.ArgumentList.Arguments.Last().Expression)
            {
                case IdentifierNameSyntax identifierName:
                    _injections.Add(new FactoryInjection(injectionId, _contextId, false, identifierName.Identifier.Text));
                    return SyntaxFactory.IdentifierName(injectionId);
                
                case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax singleVariableDesignationSyntax }:
                    _injections.Add(new FactoryInjection(injectionId, _contextId, true, singleVariableDesignationSyntax.Identifier.Text));
                    return SyntaxFactory.IdentifierName(injectionId);
            }
        }

        return base.VisitInvocationExpression(invocation);
    }
}