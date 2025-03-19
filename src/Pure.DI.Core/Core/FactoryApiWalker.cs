namespace Pure.DI.Core;

sealed class FactoryApiWalker : CSharpSyntaxWalker, IFactoryApiWalker
{
    private readonly List<FactoryMeta> _meta = [];
    private readonly List<OverrideMeta> _overrides = [];
    private int _position = 1;

    public IReadOnlyCollection<FactoryMeta> Meta => _meta;

    public override void VisitInvocationExpression(InvocationExpressionSyntax invocation)
    {
        base.VisitInvocationExpression(invocation);
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
            || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            return;
        }

        switch (memberAccess.Name)
        {
            case GenericNameSyntax genericName:
                switch (genericName.Identifier.Text)
                {
                    case nameof(IContext.Inject)
                        when invocation.ArgumentList.Arguments.Count is 1 or 2
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName):
                        _meta.Add(new FactoryMeta(FactoryMetaKind.Resolver, invocation, _overrides.ToImmutableArray()));
                        _overrides.Clear();
                        break;

                    case nameof(IContext.BuildUp)
                        when invocation.ArgumentList.Arguments.Count is 1:
                        _meta.Add(new FactoryMeta(FactoryMetaKind.Initializer, invocation, _overrides.ToImmutableArray()));
                        break;

                    case nameof(IContext.Override)
                        when invocation.ArgumentList.Arguments.Count > 0
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName):
                        _overrides.Add(new OverrideMeta(_position++, invocation));
                        break;
                }

                break;

            case IdentifierNameSyntax identifierName:
                switch (identifierName.Identifier.Text)
                {
                    case nameof(IContext.Inject)
                        when invocation.ArgumentList.Arguments.Count is 1 or 2
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName):
                        _meta.Add(new FactoryMeta(FactoryMetaKind.Resolver, invocation, _overrides.ToImmutableArray()));
                        _overrides.Clear();
                        break;

                    case nameof(IContext.BuildUp)
                        when invocation.ArgumentList.Arguments.Count is 1:
                        _meta.Add(new FactoryMeta(FactoryMetaKind.Initializer, invocation, _overrides.ToImmutableArray()));
                        _overrides.Clear();
                        break;

                    case nameof(IContext.Override)
                        when invocation.ArgumentList.Arguments.Count > 0
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName):
                        _overrides.Add(new OverrideMeta(_position++, invocation));
                        break;
                }

                break;
        }
    }
}