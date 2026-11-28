// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core.Incremental;

using System.Runtime.CompilerServices;

/// <summary>
/// What of a syntax tree can change the generated code.
/// A tree with a setup changes it with any edit: factories are copied from it, and its diagnostics refer to positions in it.
/// Other trees change it only with declarations, so the bodies of members are skipped, which is where most edits happen.
/// </summary>
sealed class SyntaxTreeShape
{
    // Syntax trees that were not edited are reused by the next compilation, so the shape of a tree is calculated once.
    private static readonly ConditionalWeakTable<SyntaxTree, SyntaxTreeShape> Shapes = new();

    private SyntaxTreeShape(
        bool hasSetup,
        ulong hash,
        ImmutableArray<string> setupTypeNames,
        ImmutableArray<(string Name, ulong Hash)> partialTypes)
    {
        HasSetup = hasSetup;
        Hash = hash;
        SetupTypeNames = setupTypeNames;
        PartialTypes = partialTypes;
    }

    public bool HasSetup { get; }

    // The hash of the full text for a tree with a setup, and of the declarations for other trees.
    public ulong Hash { get; }

    // The names of the types that contain a setup.
    public ImmutableArray<string> SetupTypeNames { get; }

    // Members of a setup type can be copied to the composition class, also from its other partial declarations,
    // so the full text of partial types is hashed separately.
    public ImmutableArray<(string Name, ulong Hash)> PartialTypes { get; }

    public static SyntaxTreeShape Get(SyntaxTree tree, CancellationToken cancellationToken) =>
        Shapes.TryGetValue(tree, out var shape)
            ? shape
            : Shapes.GetValue(tree, _ => Create(tree, cancellationToken));

    private static SyntaxTreeShape Create(SyntaxTree tree, CancellationToken cancellationToken)
    {
        var root = tree.GetRoot(cancellationToken);
        var setupTypeNames = new List<string>();
        var hasSetup = false;
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!IsSetupInvocation(invocation))
            {
                continue;
            }

            hasSetup = true;
            foreach (var type in invocation.Ancestors().OfType<TypeDeclarationSyntax>())
            {
                setupTypeNames.Add(type.Identifier.ValueText);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (hasSetup)
        {
            return new SyntaxTreeShape(
                true,
                Hasher.Hash(tree.GetText(cancellationToken)),
                // A collection expression needs the newer System.Collections.Immutable that only the Roslyn 5.9 build references.
                // ReSharper disable once UseCollectionExpression
                setupTypeNames.Distinct().ToImmutableArray(),
                ImmutableArray<(string, ulong)>.Empty);
        }

        var hasher = new Hasher();
        foreach (var token in root.DescendantTokens(static node => !IsMemberBody(node)))
        {
            hasher.Add(token.RawKind);
            hasher.Add(token.Text);
        }

        var partialTypes = root
            .DescendantNodes(static node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax)
            .OfType<TypeDeclarationSyntax>()
            .Where(type => type.Modifiers.Any(SyntaxKind.PartialKeyword))
            .Select(type => (type.Identifier.ValueText, Hasher.Hash(type.ToFullString())))
            .ToImmutableArray();

        return new SyntaxTreeShape(false, hasher.Value, ImmutableArray<string>.Empty, partialTypes);
    }

    // The same names as Metadata.IsMetadata expects, so a tree without them has no setup.
    private static bool IsSetupInvocation(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            IdentifierNameSyntax { Identifier.ValueText: nameof(DI.Setup) } => true,
            MemberAccessExpressionSyntax { Name.Identifier.ValueText: nameof(DI.Setup) } => true,
            _ => false
        };

    private static bool IsMemberBody(SyntaxNode node) =>
        node switch
        {
            BlockSyntax { Parent: BaseMethodDeclarationSyntax or AccessorDeclarationSyntax } => true,
            ArrowExpressionClauseSyntax { Parent: BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or BasePropertyDeclarationSyntax } => true,
            _ => false
        };
}
