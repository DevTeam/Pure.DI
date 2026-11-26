// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable once CheckNamespace
// The type is a part of the public generator API, so it lives in the root namespace while the files
// that implement it are grouped by purpose; the loops avoid LINQ to keep the snapshot comparison allocation-free.
namespace Pure.DI;

using Core.Incremental;

/// <summary>
/// The input of the code generation. Two snapshots are equal when they are expected to produce the same code,
/// so after an edit that cannot change the generated code, Roslyn keeps the previous sources and diagnostics
/// instead of running the generation again.
/// </summary>
public sealed class CompilationSnapshot
{
    private readonly ulong _fingerprint;

    private CompilationSnapshot(
        Compilation compilation,
        ParseOptions parseOptions,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        ImmutableArray<SyntaxTree> setupTrees,
        ulong fingerprint)
    {
        Compilation = compilation;
        ParseOptions = parseOptions;
        AnalyzerConfigOptionsProvider = analyzerConfigOptionsProvider;
        SetupTrees = setupTrees;
        _fingerprint = fingerprint;
    }

    public Compilation Compilation { get; }

    public ParseOptions ParseOptions { get; }

    public AnalyzerConfigOptionsProvider AnalyzerConfigOptionsProvider { get; }

    // The syntax trees that contain a setup, all other trees have no metadata for the generation.
    public ImmutableArray<SyntaxTree> SetupTrees { get; }

    public static CompilationSnapshot Create(
        Compilation compilation,
        ParseOptions parseOptions,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        CancellationToken cancellationToken)
    {
        var shapes = new List<(SyntaxTree Tree, SyntaxTreeShape Shape)>();
        var setupTrees = ImmutableArray.CreateBuilder<SyntaxTree>();
        var setupTypeNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var shape = SyntaxTreeShape.Get(tree, cancellationToken);
            shapes.Add((tree, shape));
            if (!shape.HasSetup)
            {
                continue;
            }

            setupTrees.Add(tree);
            setupTypeNames.UnionWith(shape.SetupTypeNames);
        }

        var hasher = new Hasher();
        hasher.Add(compilation.AssemblyName);
        foreach (var (tree, shape) in shapes)
        {
            hasher.Add(tree.FilePath);
            hasher.Add(shape.HasSetup);
            hasher.Add(shape.Hash);
            foreach (var (name, hash) in shape.PartialTypes)
            {
                if (setupTypeNames.Contains(name))
                {
                    hasher.Add(hash);
                }
            }
        }

        return new CompilationSnapshot(compilation, parseOptions, analyzerConfigOptionsProvider, setupTrees.ToImmutable(), hasher.Value);
    }

    // Whether the code generated for this snapshot is the same as for the previous one.
    internal bool HasSameInputs(CompilationSnapshot other) =>
        _fingerprint == other._fingerprint
        && ReferenceEquals(AnalyzerConfigOptionsProvider, other.AnalyzerConfigOptionsProvider)
        && ParseOptions.Equals(other.ParseOptions)
        && Compilation.Options.Equals(other.Compilation.Options)
        // A metadata reference is replaced when the assembly it refers to changes.
        && Compilation.References.SequenceEqual(other.Compilation.References, ReferenceEqualityComparer.Shared);

    private sealed class ReferenceEqualityComparer : IEqualityComparer<MetadataReference>
    {
        public static readonly ReferenceEqualityComparer Shared = new();

        public bool Equals(MetadataReference? x, MetadataReference? y) => ReferenceEquals(x, y);

        public int GetHashCode(MetadataReference obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

/// <summary>
/// Compares compilation snapshots to skip the code generation that cannot change the code.
/// It never skips it after a generation that reported a diagnostic outside the setups:
/// a diagnostic refers to a position in a file, and the position can change with any edit of that file.
/// </summary>
public sealed class CompilationSnapshotComparer : IEqualityComparer<CompilationSnapshot>
{
    private volatile bool _hasDiagnosticsOutsideSetups = true;

    public void OnGenerated(bool hasDiagnosticsOutsideSetups) =>
        _hasDiagnosticsOutsideSetups = hasDiagnosticsOutsideSetups;

    public bool Equals(CompilationSnapshot? x, CompilationSnapshot? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null || _hasDiagnosticsOutsideSetups)
        {
            return false;
        }

        return x.HasSameInputs(y);
    }

    public int GetHashCode(CompilationSnapshot obj) => 0;
}
