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
    private const string DISetup = $"{nameof(DI)}.{nameof(DI.Setup)}";
    private readonly Stack<InvocationExpressionSyntax> _invocations = new();
    private string _namespace = string.Empty;
    private bool _isMetadata;

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
    public void Visit(IMetadataVisitor metadataVisitor, in SyntaxUpdate update)
    {
        Visit(update.Node);
        var invocations = new Stack<InvocationExpressionSyntax>();
        while (_invocations.TryPop(out var invocation))
        {
            invocations.Push(invocation);
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

        while (invocations.TryPop(out var invocation))
        {
            invocationProcessor.ProcessInvocation(metadataVisitor, update.SemanticModel, invocation, _namespace);
        }

        metadataVisitor.VisitFinish();
    }
    
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
    
    private static string GetInvocationName(InvocationExpressionSyntax invocation) => GetName(invocation.Expression, 2);

    private static string GetName(ExpressionSyntax expression, int deepness = int.MaxValue)
    {
        switch (expression)
        {
            case IdentifierNameSyntax identifierNameSyntax:
                return identifierNameSyntax.Identifier.Text;
            
            case MemberAccessExpressionSyntax memberAccess when memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression):
            {
                var name = memberAccess.Name.Identifier.Text;
                if (--deepness > 0)
                {
                    var prefix = GetName(memberAccess.Expression, deepness);
                    return prefix == string.Empty ? name : $"{prefix}.{name}";
                }

                return name;
            }
            
            default:
                return string.Empty;
        }
    }
    
    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
    private static bool IsMetadata(InvocationExpressionSyntax invocation) =>
        invocation
            .DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(i => GetInvocationName(i) == DISetup) is not null;
}