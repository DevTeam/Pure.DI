namespace Pure.DI.Core;

sealed class FactoryApiWalker : CSharpSyntaxWalker, IFactoryApiWalker
{
    private readonly List<FactoryMeta> _meta = [];
    private int _metaPosition = 1;
    private readonly Stack<OverrideState> _overrideStates = new();
    private SemanticModel? _semanticModel;
    private IParameterSymbol? _contextSymbol;
    private LambdaExpressionSyntax? _rootLambda;
    private string? _contextName;

    public IReadOnlyCollection<FactoryMeta> Meta => _meta;

    public void Initialize(SemanticModel semanticModel, ParameterSyntax contextParameter, LambdaExpressionSyntax rootLambda)
    {
        _semanticModel = semanticModel;
        _rootLambda = rootLambda;
        _contextName = contextParameter.Identifier.Text;
        _contextSymbol = contextParameter.SyntaxTree == semanticModel.SyntaxTree
            ? semanticModel.GetDeclaredSymbol(contextParameter)
            : null;
    }

    public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
    {
        EnterLambda();
        try
        {
            base.VisitSimpleLambdaExpression(node);
        }
        finally
        {
            ExitLambda();
        }
    }

    public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
    {
        EnterLambda();
        try
        {
            base.VisitParenthesizedLambdaExpression(node);
        }
        finally
        {
            ExitLambda();
        }
    }

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
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName)
                             && IsContextIdentifier(contextIdentifierName):
                        _meta.Add(new FactoryMeta(FactoryMetaKind.Resolver, invocation, _metaPosition++, CurrentOverrides.ToImmutableArray()));
                        CurrentOverrides.Clear();
                        break;

                    case nameof(IContext.BuildUp)
                        when invocation.ArgumentList.Arguments.Count is 1:
                        _meta.Add(new FactoryMeta(FactoryMetaKind.Initializer, invocation, _metaPosition++, CurrentOverrides.ToImmutableArray()));
                        CurrentOverrides.Clear();
                        break;

                    case nameof(IContext.Override)
                        when invocation.ArgumentList.Arguments.Count > 0
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName)
                             && IsContextIdentifier(contextIdentifierName):
                        CurrentOverrides.Add(new OverrideMeta(CurrentOverridePosition++, invocation));
                        break;

                    case nameof(IContext.Let)
                        when invocation.ArgumentList.Arguments.Count > 0
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName)
                             && IsContextIdentifier(contextIdentifierName):
                        CurrentOverrides.Add(new OverrideMeta(CurrentOverridePosition++, invocation));
                        break;
                }

                break;

            case IdentifierNameSyntax identifierName:
                switch (identifierName.Identifier.Text)
                {
                    case nameof(IContext.Inject)
                        when invocation.ArgumentList.Arguments.Count is 1 or 2
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName)
                             && IsContextIdentifier(contextIdentifierName):
                        _meta.Add(new FactoryMeta(FactoryMetaKind.Resolver, invocation, _metaPosition++, CurrentOverrides.ToImmutableArray()));
                        CurrentOverrides.Clear();
                        break;

                    case nameof(IContext.BuildUp)
                        when invocation.ArgumentList.Arguments.Count is 1:
                        _meta.Add(new FactoryMeta(FactoryMetaKind.Initializer, invocation, _metaPosition++, CurrentOverrides.ToImmutableArray()));
                        CurrentOverrides.Clear();
                        break;

                    case nameof(IContext.Override)
                        when invocation.ArgumentList.Arguments.Count > 0
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName)
                             && IsContextIdentifier(contextIdentifierName):
                        CurrentOverrides.Add(new OverrideMeta(CurrentOverridePosition++, invocation));
                        break;

                    case nameof(IContext.Let)
                        when invocation.ArgumentList.Arguments.Count > 0
                             && memberAccess is { Expression: IdentifierNameSyntax contextIdentifierName }
                             && contextIdentifierName.IsKind(SyntaxKind.IdentifierName)
                             && IsContextIdentifier(contextIdentifierName):
                        CurrentOverrides.Add(new OverrideMeta(CurrentOverridePosition++, invocation));
                        break;
                }

                break;
        }
    }

    private List<OverrideMeta> CurrentOverrides => CurrentState.Overrides;

    private int CurrentOverridePosition
    {
        get => CurrentState.Position;
        set => CurrentState.Position = value;
    }

    private OverrideState CurrentState
    {
        get
        {
            if (_overrideStates.Count == 0)
            {
                _overrideStates.Push(new OverrideState());
            }

            return _overrideStates.Peek();
        }
    }

    private void EnterLambda() => _overrideStates.Push(new OverrideState());

    private void ExitLambda()
    {
        if (_overrideStates.Count > 0)
        {
            _overrideStates.Pop();
        }
    }

    private bool IsContextIdentifier(IdentifierNameSyntax identifierName)
    {
        if (_contextName is null || identifierName.Identifier.Text != _contextName)
        {
            return false;
        }

        var nearestLocalFunction = identifierName
            .Ancestors()
            .OfType<LocalFunctionStatementSyntax>()
            .FirstOrDefault(localFunction => localFunction.ParameterList.Parameters.Any(parameter => parameter.Identifier.Text == identifierName.Identifier.Text));

        if (nearestLocalFunction is not null)
        {
            return false;
        }

        var nearestLambda = identifierName
            .AncestorsAndSelf()
            .OfType<LambdaExpressionSyntax>()
            .FirstOrDefault(lambda => LambdaDeclaresParameter(lambda, identifierName.Identifier.Text));

        if (nearestLambda is not null && !ReferenceEquals(nearestLambda, _rootLambda))
        {
            return false;
        }

        // ReSharper disable once InvertIf
        if (_semanticModel is not null
            && _contextSymbol is not null
            && identifierName.SyntaxTree == _semanticModel.SyntaxTree)
        {
            var symbol = _semanticModel.GetSymbolInfo(identifierName).Symbol;
            return symbol is not null && SymbolEqualityComparer.Default.Equals(symbol, _contextSymbol);
        }

        return nearestLambda is null || ReferenceEquals(nearestLambda, _rootLambda);
    }

    private static bool LambdaDeclaresParameter(LambdaExpressionSyntax lambda, string name) =>
        lambda switch
        {
            SimpleLambdaExpressionSyntax { Parameter.Identifier.Text: var parameterName } => parameterName == name,
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters.Any(parameter => parameter.Identifier.Text == name),
            _ => false
        };

    private sealed class OverrideState
    {
        public List<OverrideMeta> Overrides { get; } = [];

        public int Position { get; set; } = 1;
    }
}
