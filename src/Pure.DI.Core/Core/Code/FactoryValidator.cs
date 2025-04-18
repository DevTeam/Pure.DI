﻿namespace Pure.DI.Core.Code;

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
                throw new CompileErrorException(string.Format(Strings.Error_Template_CannotUseContextDirectly, _contextParameterName), node.GetLocation(), LogId.ErrorInvalidMetadata);
            }
        }

        base.VisitIdentifierName(node);
    }
}