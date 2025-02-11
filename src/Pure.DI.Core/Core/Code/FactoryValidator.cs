namespace Pure.DI.Core.Code;

internal sealed class FactoryValidator(DpFactory factory) : CSharpSyntaxWalker
{
    private readonly string _contextParameterName = factory.Source.Context.Identifier.Text;

    public void Validate(LambdaExpressionSyntax lambda) => Visit(lambda);

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (_contextParameterName == node.Identifier.Text)
        {
            if (node.Parent is ArgumentSyntax)
            {
                throw new CompileErrorException($"It is not possible to use \"{_contextParameterName}\" directly. Only its methods or properties can be used.", node.GetLocation(), LogId.ErrorInvalidMetadata);
            }
        }

        base.VisitIdentifierName(node);
    }
}