# Pure.DI Code Generation Performance Optimization Plan

## Objective

Reduce the CPU time required to generate code for `HugeComposition` without changing its generated API, supported Pure.DI patterns, profile scale, or diagnostics.

The optimization process uses the XML reports produced by:

```powershell
dotnet run --project .\build -- codegen-performance
```

The measurement and report format are described in [PERFORMANCE_TESTING.md](PERFORMANCE_TESTING.md).
Track the status and evidence for each optimization in [PERFORMANCE_OPTIMIZATION_PROGRESS.md](PERFORMANCE_OPTIMIZATION_PROGRESS.md).

## Current workloads

The `AllPatterns` workload contains:

- 1,311 bindings;
- 28 roots;
- 2,560 generated service declarations;
- four internal bulk setups merged into one public composition;
- the feature compositions listed in [USAGE_PATTERNS.md](USAGE_PATTERNS.md).

Six focused profiles separately stress bindings, roots, declarations, factories and tags, scopes and accumulators, and generic variants. Their definitions are documented in [PERFORMANCE_TESTING.md](PERFORMANCE_TESTING.md).

Do not reduce profile sizes during optimization. A workload change starts a new local comparison series and cannot be compared directly with older reports.

## Establishing a baseline

The baseline is deliberately not stored in this document. Absolute profiler values depend on the CPU, memory, operating system, .NET SDK, power mode, thermal state, and concurrent workload.

Create a fresh baseline on the machine used for every optimization series:

1. Keep the source revision and workload unchanged.
2. Close unrelated CPU-intensive applications.
3. Run the profiling target at least three times to create three complete report sets.
4. Calculate the median for the same named profile's generator total and relevant method subtrees.
5. Record the spread between runs to define the noise threshold.
6. Keep the resulting XML files together with the candidate reports for that local optimization series.

Do not compare absolute values from different machines. Historical reports may help identify candidate methods, but they are not a performance baseline.

## Measurement protocol

For every optimization candidate:

1. Keep the machine, .NET SDK, power mode, configuration, and workload unchanged.
2. Create three baseline report sets before the first code change.
3. Record the median `TotalTime` and `OwnTime` for the targeted method and its parent subtree.
4. Make one focused implementation change.
5. Build the standalone solution and run the relevant functional tests.
6. Create three candidate report sets.
7. Compare medians within each named profile by matching `Function/@FQN`.
8. Keep the change only when the improvement is larger than baseline variation and appears in both the targeted method and its parent subtree.

Sampling data is intended for relative comparison. Do not treat its absolute duration as normal build time.

Use the focused profile associated with the changed subsystem as the primary signal. Also inspect `AllPatterns` to reject local improvements that regress the representative end-to-end workload.

## Optimization sequence

After creating a fresh baseline, rank Pure.DI methods by `OwnTime` and `TotalTime`. Start with the highest-cost subtree that contains an actionable redundant operation. The areas below are investigation routes, not a fixed priority order.

### Reduce Roslyn semantic queries

Use this route when `Semantic.GetTypeSymbolCore`, `Semantic.ResolveTypeSymbol`, or their `ApiInvocationProcessor` parent subtree is prominent in the fresh baseline.

Investigate:

- which syntax nodes passed to `Semantic.GetTypeSymbolCore` are resolved more than once;
- whether a single invocation causes separate semantic queries for its contract and implementation types;
- whether generic type arguments can be obtained from one resolved `IMethodSymbol` instead of individual `GetTypeInfo` calls;
- why nodes from a syntax tree different from the current `SemanticModel` bypass the existing cache;
- whether the cache lifetime covers all setup groups processed in one generator invocation;
- whether syntax-only fast paths can handle known Pure.DI API forms before Roslyn semantic resolution.

Candidate changes should prefer fewer Roslyn calls over replacing one cache implementation with another. Add temporary investigation logging only in a local profiling branch; do not add profiling code to the production generator.

Success criteria:

- at least 10% reduction in median `Semantic.GetTypeSymbolCore.TotalTime`;
- reduction in `ApiInvocationProcessor.ProcessInvocation.TotalTime`;
- unchanged diagnostics and generated source.

### Streamline API invocation processing

Use this route when `ApiInvocationProcessor.ProcessInvocation` remains prominent after excluding time spent in its Roslyn semantic-query children.

Investigate:

- repeated traversal of invocation arguments and trivia;
- repeated `ToList`, `SkipWhile`, `Select`, `Last`, and immutable-array construction in common binding forms;
- repeated string comparisons and `Enum.TryParse` calls for known API method names;
- repeated construction of tags and contracts for lifetime-specific bindings;
- whether the common `.Bind<T>().As(...).To<T>()` path can be handled without general-purpose factory and tag logic.

Prefer explicit fast paths for frequent DSL forms while keeping the existing general path for advanced features.

Success criteria:

- lower `ProcessInvocation.OwnTime` and `TotalTime` after excluding improvements already obtained from semantic resolution;
- no feature-specific regression in the patterns exercised by `HugeComposition`.

### Optimize dependency graph construction

Focus on `DependencyGraphBuilder.Build` and `VariationalDependencyGraphBuilder.Build`.

Investigate:

- repeated creation and hashing of `Injection` and processing-node keys;
- repeated scans of maps for generic and marker-based bindings;
- queue entries that are processed more than once;
- temporary `HashSet`, `Dictionary`, list, and LINQ allocations sized from known graph counts;
- whether maps can be populated once and reused by later graph phases;
- whether generic-binding lookup can be indexed instead of using ordered scans.

Use collection sizes from the current workload to validate any proposed index. Avoid an index whose setup cost is greater than the saved lookup cost for ordinary compositions.

Success criteria:

- at least 10% reduction in the targeted graph-builder subtree;
- no increase in unresolved-binding diagnostics or graph-validation time;
- no material regression on small compositions.

### Reduce repeated graph traversal

Focus on `GraphWalker.Walk`, cyclic-dependency validation, and other visitors that traverse the same dependency graph.

Investigate:

- whether multiple validators perform equivalent walks;
- the cost of `ProcessedKey` hashing and `ImmutableArray<int>` creation;
- `SequenceEqual` and repeated dependency-index copying;
- whether non-lazy paths need the full dependency-index history;
- whether traversal results can be shared safely between read-only validation phases.

Success criteria:

- lower `GraphWalker.Walk.OwnTime` and all affected visitor subtrees;
- identical cycle detection and validation diagnostics.

### Optimize lazy-node detection

`NodeTools.IsLazy` has the highest observed own time, although its total contribution is smaller than the metadata and graph phases.

Investigate:

- cache hit and miss rates for `LazyKey`;
- repeated dictionary lookups for the same binding and semantic model;
- delegate allocation caused by the cache factory on a hot path;
- repeated `GetTypeInfo` calls inside factory lazy detection;
- whether laziness can be computed once when dependency nodes are created.

Prioritize this work only when a fresh report shows that it is significant relative to the complete generator run.

Success criteria:

- at least 20% reduction in `NodeTools.IsLazy.OwnTime`;
- no changes in generated factories, delegates, enumerable handling, or disposal behavior.

### Cache attribute matching

Focus on `Attributes.GetAttributes` and `Attributes.GetAttribute`.

Investigate:

- repeated calls to `ISymbol.GetAttributes()` for the same symbol;
- repeated construction of unbound generic symbols;
- repeated global-name formatting of both the discovered and expected attribute types;
- avoidable `Where(...).ToList()` allocation when only zero, one, or multiple matches are required;
- using the existing Pure.DI type-symbol comparer instead of formatted names where semantics permit it.

Success criteria:

- lower attribute lookup own time and allocation count;
- identical handling of generic attributes, invalid positions, and duplicate attributes.

### Reduce generated-code construction overhead

After metadata and graph work, profile `CompositionBuilder`, `RootBuilder`, `RootCodeBuilder`, and implementation/factory code builders again.

Investigate:

- repeated cloning of `CodeContext` and `Parents` arrays;
- repeated formatting and buffering of the same type, variable, and tag names;
- unnecessary intermediate `Lines`, immutable arrays, and strings;
- whether code fragments common to several roots can be generated once without changing the public API;
- repeated `NodeTools.IsLazy` and disposable checks during root generation.

Success criteria:

- reduction in `CodeBuilder.BuildCode` and root-builder subtrees;
- byte-equivalent generated behavior and successful compilation of all roots.

## Correctness checks

During initial profiler development, a successful standalone build is sufficient. Once generator code is changed, validate proportionally to the affected area.

Run the first two checks for every accepted optimization. Run the full integration suite periodically before accepting a notable series of changes, and earlier when a candidate affects integration-sensitive behavior. Do not run the full suite for every optimization iteration.

```powershell
dotnet build .\samples\HugeComposition\HugeComposition.slnx --no-restore
dotnet test .\tests\Pure.DI.Tests\Pure.DI.Tests.csproj --no-restore
dotnet test .\tests\Pure.DI.IntegrationTests\Pure.DI.IntegrationTests.csproj --no-restore
```

Also run focused usage tests for modified DSL parsing, lifetimes, tags, factories, graph validation, or generated code.

## Regression safeguards

- Keep a small-composition control measurement so optimizations for `HugeComposition` do not penalize normal projects.
- Do not cache Roslyn symbols beyond the owning `Compilation` or `SemanticModel` lifetime.
- Use Roslyn-aware symbol comparers; do not replace semantic equality with string equality unless the format is an established identity key.
- Do not parallelize generator phases until shared caches, deterministic ordering, cancellation, and source emission are proven thread-safe.
- Do not accept reduced generated-code size when it changes roots, lifetime semantics, disposal, or diagnostics.
- Treat improvements below 5% as inconclusive unless baseline variance is demonstrably smaller.
- Reject an optimization that materially complicates a hot path unless profiling demonstrates a substantial, repeatable local benefit.

## Completion criteria

The optimization cycle is complete when all of the following are true:

- three candidate runs show a repeatable improvement over three baseline runs;
- total generator time is reduced by at least 20% for the unchanged workload;
- no remaining hotspot has both more than 10% of generator total time and an actionable redundant operation;
- the standalone solution and relevant tests pass;
- small-composition performance has not materially regressed;
- generated code and diagnostics remain correct.
