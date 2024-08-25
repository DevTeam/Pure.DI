// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable TailRecursiveCall
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable InvertIf
// ReSharper disable HeapView.BoxingAllocation

namespace Pure.DI.Core;

internal sealed class MetadataSyntaxWalker(
    IApiInvocationProcessor invocationProcessor,
    CancellationToken cancellationToken)
    : CSharpSyntaxWalker, IMetadataSyntaxWalker
{
    private readonly Stack<InvocationExpressionSyntax> _invocations = new();
    private string _namespace = string.Empty;
    private bool _isMetadata;
    private SemanticModel? _semanticModel;
    
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
    public void Visit(IMetadataVisitor metadataVisitor, in SyntaxUpdate update)
    {
        _semanticModel = update.SemanticModel;
        Visit(update.Node);
        var visitors = new List<InvocationVisitor>();
        while (_invocations.TryPop(out var invocation))
        {
            visitors.Add(new InvocationVisitor(update.SemanticModel, invocation, metadataVisitor, cancellationToken));
            _isMetadata = true;
            try
            {
                base.VisitInvocationExpression(invocation);
            }
            finally
            {
                _isMetadata = false;
            }
        }

        if (visitors.Count == 0)
        {
            return;
        }
        
        visitors.Reverse();
#if DEBUG
        visitors.ForEach(i => ProcessInvocation(i));
#else
        Parallel.ForEach(visitors, new ParallelOptions() { CancellationToken = cancellationToken }, i => ProcessInvocation(i));
#endif
        foreach (var visitor in visitors)
        {
            visitor.Apply();
        }
        
        metadataVisitor.VisitFinish();
    }

    private void ProcessInvocation(in InvocationVisitor visitor) => 
        invocationProcessor.ProcessInvocation(visitor, visitor.SemanticModel, visitor.Invocation, _namespace);

    // ReSharper disable once CognitiveComplexity
    public override void VisitInvocationExpression(InvocationExpressionSyntax invocation)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_isMetadata || IsMetadata(invocation))
        {
            _invocations.Push(invocation);
        }
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax namespaceDeclaration)
    {
        _namespace = namespaceDeclaration.Name.ToString().Trim();
        base.VisitFileScopedNamespaceDeclaration(namespaceDeclaration);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration)
    {
        _namespace = namespaceDeclaration.Name.ToString().Trim();
        base.VisitNamespaceDeclaration(namespaceDeclaration);
    }

    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
    private bool IsMetadata(InvocationExpressionSyntax invocation)
    {
        foreach (var curInvocation in invocation.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Reverse())
        {
            switch (curInvocation.Expression)
            {
                case IdentifierNameSyntax { Identifier.Text: nameof(DI.Setup) }:
                case MemberAccessExpressionSyntax memberAccess 
                    when memberAccess.Kind() == SyntaxKind.SimpleMemberAccessExpression
                    && memberAccess.Name.Identifier.Text == nameof(DI.Setup)
                    && (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: nameof(DI) } 
                        || memberAccess.Expression is MemberAccessExpressionSyntax firstMemberAccess && firstMemberAccess.Kind() == SyntaxKind.SimpleMemberAccessExpression && firstMemberAccess.Name.Identifier.Text == nameof(DI)):
                    
                    if (_semanticModel?.GetTypeInfo(curInvocation) is { } typeInfo
                        && (typeInfo.Type ?? typeInfo.ConvertedType) is { } type
                        && type.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat) == Names.ConfigurationInterfaceName)
                    {
                        return true;
                    }
                    
                    break;
            }
        }

        return false;
    }
}