# Pure.DI Code Generation Performance Testing

## Purpose

`HugeComposition` is the workload for finding and verifying performance bottlenecks in the Pure.DI source generator. Its supported dependency-injection patterns are documented in [USAGE_PATTERNS.md](USAGE_PATTERNS.md).

The current workload contains 1,311 bindings, 28 roots, and 2,560 generated service declarations. Its bulk bindings are distributed across four internal setups and merged into one public composition through `DependsOn`.

The performance target profiles compilation of:

```text
samples/HugeComposition/HugeComposition.csproj
```

This is a code-generation profiling process. It is separate from the runtime benchmarks executed by the `performance` target.
`HugeComposition` is intentionally excluded from `Pure.DI.slnx`; build the standalone solution when working with this workload:

```powershell
dotnet build .\samples\HugeComposition\HugeComposition.slnx
```

## Run

From the repository root:

```powershell
dotnet run --project .\build -- codegen-performance
```

Short aliases are also available:

```powershell
dotnet run --project .\build -- codegen-perf
dotnet run --project .\build -- cgp
```

The target uses the `JetBrains.dotTrace.CommandLineTools.windows-x64` package restored for the build project. A global dotTrace installation is not required.

## What the target does

1. Builds `HugeComposition` once to prepare its project dependencies.
2. Runs a non-incremental Release compilation under dotTrace in `Sampling` mode.
3. Disables compiler-server and MSBuild-node reuse and uses one MSBuild worker.
4. Profiles child processes so the compiler process executing the source generator is included.
5. Uses dotTrace Reporter patterns for `Pure.DI.SourceGenerator.*` and `Pure.DI.Core.*`.
6. Generates an XML report with full signatures and call stacks.
7. Deletes the temporary snapshot and pattern file.

All external processes are launched through the build application's `ICommandLineRunner`.

## Output

Every invocation creates exactly one persistent artifact:

```text
.logs/code-generation-performance/yyyyMMdd-HHmmssfff.xml
```

The report path is printed after a successful run. Timestamped names preserve the baseline and candidate reports from successive optimization iterations.

The XML is produced directly by JetBrains `Reporter.exe`. Its `Function` elements contain:

- `FQN`: fully qualified method name and signature;
- `TotalTime`: time in the method and its callees;
- `OwnTime`: time spent directly in the method;
- `Calls`: invocation count when it is available for the selected profiling mode;
- `Instance`: call-stack-specific measurements when available.

The report also contains process information, which helps distinguish compiler child processes from the outer build process.

## Optimization iteration

Use the following process for each performance change.

### 1. Create a baseline

Run the target without changing the generator and keep the resulting XML path. Prefer two or three runs to learn the normal variation on the current machine.

### 2. Select a bottleneck

Inspect functions under `Pure.DI.Core`:

- sort primarily by `OwnTime` to find expensive implementation code;
- use `TotalTime` to find expensive subtrees;
- use `Calls`, when available, to distinguish a slow operation from a cheap operation repeated too often;
- inspect `Instance` call stacks to verify that the cost belongs to the Pure.DI generation path.

Do not optimize a method solely because it has high inclusive time. First identify which child method or repeated operation accounts for that time.

### 3. Make one focused change

Change the smallest relevant generator path. Do not reduce `HugeComposition` coverage, binding count, root count, declaration count, or generated-code correctness to improve the result.

Keep unrelated refactoring out of the same iteration so the report remains attributable to one change.

### 4. Generate the candidate report

Run the same target again. Compare the new XML with the baseline by matching `Function/@FQN` and reviewing changes in `OwnTime`, `TotalTime`, and `Calls`.

An improvement is credible when:

- the targeted method or subtree becomes faster;
- the cost is not merely moved to another Pure.DI method;
- the result repeats in another candidate run;
- the change is larger than the baseline run-to-run variation.

### 5. Continue or revert

If the result is repeatable, keep the change and use the candidate report as the next baseline. Otherwise revert only that optimization attempt and choose another hotspot.

## Comparison rules

- Compare reports created on the same machine, power mode, .NET SDK, configuration, and workload revision.
- Close unrelated CPU-intensive applications before profiling.
- dotTrace sampling adds overhead. Treat the values as comparative profiling data, not as normal build duration.
- Prefer changes visible in several runs; small differences are usually noise.
- When call counts are present, they should normally remain stable for an unchanged workload. Unexpected changes require investigation.
- Generated-code size and behavior must remain valid even when a change improves profiler results.

## Verification policy

While the profiling process itself is being developed, it is sufficient to verify that:

- the build target compiles;
- `HugeComposition` compiles under dotTrace;
- Reporter creates a valid, non-empty XML report containing Pure.DI functions.

After an actual generator optimization, run the relevant functional tests before accepting the change. The exact test scope depends on the modified generator component; performance improvement alone is not sufficient evidence of correctness.
