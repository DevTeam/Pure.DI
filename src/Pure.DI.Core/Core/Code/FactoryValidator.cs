namespace Pure.DI.Core.Code;

sealed class FactoryValidator : CSharpSyntaxWalker, IFactoryValidator
{
    private string? _contextParameterName;

    public IFactoryValidator Initialize(DpFactory factory)
    {
        _contextParameterName = factory.Source.Context.Identifier.Text;
        return this;
    }

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