namespace Pure.DI.Core.Code;

sealed class FactoryValidator(ILocationProvider locationProvider, DpFactory factory)
    : CSharpSyntaxWalker, IFactoryValidator
{
    private readonly string _contextParameterName = factory.Source.Context.Identifier.Text;

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (_contextParameterName == node.Identifier.Text)
        {
            if (node.Parent is ArgumentSyntax)
            {
                throw new CompileErrorException(
                    string.Format(Strings.Error_Template_CannotUseContextDirectly, _contextParameterName),
                    ImmutableArray.Create(locationProvider.GetLocation(node)),
                    LogId.ErrorCannotUseContextDirectly,
                    nameof(Strings.Error_Template_CannotUseContextDirectly));
            }
        }

        base.VisitIdentifierName(node);
    }
}
