# Pure.DI Code Generation Performance Testing

## Purpose

`HugeComposition` is an isolated, scalable workload for locating performance bottlenecks in the Pure.DI source generator. Supported dependency-injection patterns are listed in [USAGE_PATTERNS.md](USAGE_PATTERNS.md).

The workload is intentionally excluded from `Pure.DI.slnx`. Its standalone solution can be built with:

```powershell
dotnet build .\samples\HugeComposition\HugeComposition.slnx
```

This process measures source generation during compilation. It is separate from the runtime benchmarks executed by the `performance` target.

## Run

From the repository root:

```powershell
dotnet run --project .\build -- codegen-performance
```

The aliases `codegen-perf` and `cgp` are also available. The target restores the repository-local `dotnet-t4` tool and uses `JetBrains.dotTrace.CommandLineTools.windows-x64` from the build project. Global installations are not required.

## Profiles

One invocation generates and profiles eight compositions from [Composition.tt](Composition.tt). Each profile isolates a different scaling dimension or generator subsystem.

| Profile | Generated scale | Purpose |
|---|---:|---|
| `AllPatterns` | 1,311 bindings; 28 roots; 2,560 declarations | Representative connected composition covering the broad Pure.DI feature set. |
| `Bindings` | 2,413 bindings; 38 roots; 4,826 declarations | Binding parsing and graph growth with bounded module roots. |
| `Roots` | 146 bindings; 512 roots; 292 declarations | Root validation and code emission over a moderately sized graph. |
| `Declarations` | 0 bindings; 1 root; 1,600 declarations | Auto-binding discovery and emission over a balanced eight-way graph. |
| `FactoriesAndTags` | 399 bindings; 1 root; 401 declarations | Simplified factories, unique collection bindings, and tagged injection. |
| `ScopesAndAccumulators` | 401 bindings; 1 root; 403 declarations | Scoped lifetimes, collections, and accumulator generation. |
| `MultiTypeLifetimes` | 2,400 bindings; 1 root; 2,401 declarations | Multi-type `Transient`, `Singleton`, `Scoped`, `PerResolve`, and `PerBlock` API processing. |
| `GenericsAndVariants` | 400 bindings; 1 root; 801 declarations | Marker-based open generics expanded into many closed graphs. |

These sizes are workload definitions, not a machine-specific baseline. They are derived from the profile parameters in the build target. Change them only as an intentional workload revision. Reports from different profile definitions are not directly comparable.

The normal checked-in `Composition.cs` remains the `AllPatterns` development workload. During profiling, the target generates a temporary source file for each profile and passes it to the exact project through `HugeCompositionSource`.

## What the target does

1. Restores the local T4 tool and prepares `HugeComposition` dependencies.
2. Generates a temporary composition source for each profile.
3. Runs a clean, single-worker compilation of `HugeComposition.csproj` under dotTrace sampling.
4. Disables compiler-server and MSBuild-node reuse and profiles compiler child processes.
5. Limits Reporter output to `Pure.DI.SourceGenerator.*` and `Pure.DI.Core.*` call stacks.
6. Writes and validates one XML report per profile.
7. Deletes temporary generated sources, snapshots, and pattern files.

All external processes are launched through the build application's `ICommandLineRunner`. No profiling instrumentation is added to the generator.

## Output

Every invocation creates one timestamped report set:

```text
.logs/code-generation-performance/yyyyMMdd-HHmmssfff/
  AllPatterns.xml
  Bindings.xml
  Roots.xml
  Declarations.xml
  FactoriesAndTags.xml
  ScopesAndAccumulators.xml
  MultiTypeLifetimes.xml
  GenericsAndVariants.xml
```

The directory path is printed after a successful run. Each file is produced by JetBrains `Reporter.exe` and contains only the selected Pure.DI functions. Important XML attributes are:

- `FQN`: fully qualified method name and signature;
- `TotalTime`: time in the method and its callees;
- `OwnTime`: time spent directly in the method;
- `Calls`: invocation count when available in sampling mode;
- `Instance`: call-stack-specific measurements when available.

## Optimization iteration

Do not store a universal baseline: profiler values depend on hardware and machine state. Create a fresh local series for the current revision.

1. Run the target two or three times before changing the generator.
2. Compare the same named profile across runs and determine normal variation.
3. Use `OwnTime`, `TotalTime`, and call stacks to select a bottleneck. Use the focused profile to form the hypothesis and `AllPatterns` to detect end-to-end regressions.
4. Make one small generator change without reducing workload coverage or scale.
5. Run the standalone build and relevant tests.
6. Generate the same number of candidate report sets.
7. Keep the change only when the improvement repeats beyond normal variation and the cost is not merely moved elsewhere.

Never compare `Bindings.xml` with `Roots.xml`, or reports created from different profile definitions. Compare report sets only on the same machine, SDK, configuration, power mode, and workload revision.

## Verification policy

For changes to this profiling infrastructure, verify that:

- the build target compiles;
- every generated profile compiles;
- the target produces all seven non-empty XML reports containing Pure.DI functions.

For generator optimizations, run the relevant functional tests for every accepted change. Run the full integration suite periodically and whenever a change affects a broadly shared or integration-sensitive path. Performance evidence never replaces correctness tests.
