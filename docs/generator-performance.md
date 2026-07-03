# Optimizing the Pure.DI source generator

This guide describes a repeatable workflow for finding and validating performance optimizations in the Pure.DI C# source generator.

The main rule is simple: optimize only after a measured baseline exists, and keep the benchmark project, build flags, profiler settings, and validation commands stable while comparing changes.

## Recommended test project

Use `samples\HugeComposition\HugeComposition.csproj` as the default stress project. It contains a large generated composition and is useful for exposing generator overhead in graph discovery, metadata analysis, dependency graph building, and code generation.

Other samples can be useful when optimizing a feature-specific path:

- Type and member attributes: use a sample or test that configures `TypeAttribute`, `TagAttribute`, `LifetimeAttribute`, or exported members heavily.
- Generic and marker-heavy graphs: use a sample with many generic bindings and marker substitutions.
- Factory-heavy graphs: use a sample with `ctx.Inject`, initializers, overrides, and simple lambda factories.

## Prepare a stable measurement

Build the analyzer first, then measure only the sample project. This keeps analyzer compilation noise out of the sample-build timing.

```powershell
dotnet restore samples\HugeComposition\HugeComposition.csproj -v:quiet
dotnet build src\Pure.DI\Pure.DI.csproj -c Release -v:quiet /nr:false /p:UseSharedCompilation=false
```

For each measured run, clean the sample and build it without restore:

```powershell
dotnet clean samples\HugeComposition\HugeComposition.csproj -c Release -v:quiet
dotnet build samples\HugeComposition\HugeComposition.csproj -c Release -v:quiet --no-restore /nr:false /p:UseSharedCompilation=false
```

Use several runs, not one. Five cold sample builds are usually enough to see whether a change is clearly faster or just noise.

```powershell
$times = @()
for ($i = 1; $i -le 5; $i++) {
    dotnet clean samples\HugeComposition\HugeComposition.csproj -c Release -v:quiet | Out-Null
    $sw = [Diagnostics.Stopwatch]::StartNew()
    dotnet build samples\HugeComposition\HugeComposition.csproj -c Release -v:quiet --no-restore /nr:false /p:UseSharedCompilation=false | Out-Null
    $exit = $LASTEXITCODE
    $sw.Stop()
    $value = [math]::Round($sw.Elapsed.TotalSeconds, 3)
    $times += $value
    Write-Host "run$i=${value}s exit=$exit"
}

$avg = [math]::Round(($times | Measure-Object -Average).Average, 3)
Write-Host "avg=$avg"
```

Important flags:

- `/nr:false` disables MSBuild node reuse, reducing cross-run process state.
- `/p:UseSharedCompilation=false` disables the compiler server, making generator work easier to attribute.
- `--no-restore` keeps NuGet restore out of the measurement.

## Profile with dotTrace

Install or locate dotTrace on the machine, then point `$DotTraceHome` to its directory. Do not hard-code a machine-specific path in reusable scripts or docs.

```powershell
$DotTraceHome = "<path-to-dottrace-directory>"
$DotTrace = Join-Path $DotTraceHome "dottrace.exe"
$Reporter = Join-Path $DotTraceHome "Reporter.exe"
```

Create an output directory and a Reporter pattern file:

```powershell
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$dir = Join-Path (Resolve-Path .logs) "perf-$stamp"
New-Item -ItemType Directory -Force $dir | Out-Null

$pattern = Join-Path $dir "pattern.xml"
@'
<Patterns>
  <Pattern>Pure\.DI\..*</Pattern>
  <Pattern>Microsoft\.CodeAnalysis\..*</Pattern>
</Patterns>
'@ | Set-Content -Encoding ASCII $pattern
```

Run dotTrace around the sample build:

```powershell
$snapshot = Join-Path $dir "snapshot.dtp"
$report = Join-Path $dir "report.xml"

dotnet build src\Pure.DI\Pure.DI.csproj -c Release -v:quiet /nr:false /p:UseSharedCompilation=false | Out-Null
dotnet clean samples\HugeComposition\HugeComposition.csproj -c Release -v:quiet | Out-Null

& $DotTrace start `
    --profiling-type=Sampling `
    --time-measurement=ThreadTime `
    "--save-to=$snapshot" `
    --overwrite `
    --profile-child=* `
    --no-check-for-updates `
    --propagate-exit-code `
    "--work-dir=$(Resolve-Path .)" `
    (Get-Command dotnet).Source `
    -- build samples\HugeComposition\HugeComposition.csproj -c Release -v:minimal --no-restore /nr:false /p:UseSharedCompilation=false

if ($LASTEXITCODE -eq 0) {
    & $Reporter report (Join-Path $dir "snapshot*.dtp") "--pattern=$pattern" "--save-to=$report" --overwrite --save-signature --no-check-for-updates
}
```

dotTrace may create multiple snapshots because MSBuild starts child processes. Keep all generated `snapshot*.dtp` files and feed the wildcard to Reporter.

## Read the report

The XML report is easier to scan from PowerShell:

```powershell
[xml]$xml = Get-Content $report
$xml.Report.Function |
    Where-Object { $_.FQN -like "Pure.DI*" } |
    Sort-Object { [int]$_.TotalTime } -Descending |
    Select-Object -First 40 `
        @{n="Total";e={[int]$_.TotalTime}},
        @{n="Own";e={[int]$_.OwnTime}},
        @{n="Samples";e={[int]$_.Samples}},
        FQN |
    Format-Table -AutoSize
```

Start with `TotalTime` to identify expensive call paths, then use `OwnTime` to find methods that spend time in their own body rather than only in children.

Common areas to inspect:

- `SourceGenerator.Initialize(...)` and incremental pipeline nodes: too-broad syntax providers can create many redundant `GeneratorSyntaxContext` values.
- `MetadataWalker` and `Metadata.IsMetadata(...)`: semantic model calls are expensive; prefer syntactic filters before semantic lookups when behavior allows it.
- `SetupsBuilder.Build(...)` and metadata finalization: avoid repeated full-tree scans unless the setup actually needs them.
- `DependencyGraphBuilder.Build(...)`: look for repeated sorting, repeated dictionary scans, and generic or marker resolution loops.
- `CodeBuilder` and root/code part builders: watch for repeated formatting, repeated type-name resolution, and avoidable allocations in large graphs.

## Optimization rules

Keep generator optimizations conservative:

- Preserve compile-time diagnostics and generated code shape unless the task explicitly changes behavior.
- Prefer reducing work before caching work.
- Put cheap syntactic predicates before semantic model calls.
- Avoid broad incremental generator inputs when the downstream code only needs one update per syntax tree.
- Do not optimize sample-specific behavior if it weakens normal production scenarios.
- Measure after each isolated change; if two changes are made together, it becomes harder to know which one helped.

Example of a good optimization pattern:

1. A syntax provider currently accepts all syntax nodes.
2. Downstream code deduplicates to one update per `SyntaxTree`.
3. Narrow the provider to `CompilationUnitSyntax`.
4. Verify generated output and diagnostics still match.
5. Compare five-run averages and dotTrace reports.

Example of another good pattern:

1. A method checks direct `DI.Setup(...)` syntax.
2. It performs a semantic return-type lookup before accepting the direct syntax.
3. Accept the direct syntax syntactically and keep semantic fallback for indirect cases.
4. Verify tests and profile the metadata path again.

## Validate correctness

At minimum, validate the stress sample and the focused test project:

```powershell
dotnet build samples\HugeComposition\HugeComposition.csproj -c Release -v:minimal /nr:false /p:UseSharedCompilation=false
dotnet test tests\Pure.DI.Tests\Pure.DI.Tests.csproj -c Release --no-restore -v:minimal /nr:false /p:UseSharedCompilation=false
```

For changes in metadata parsing, attributes, graph construction, lifetimes, tags, or generated output, also run the relevant integration or usage tests.

## Report template

Save a short report under `.logs` for each optimization pass:

```markdown
# Pure.DI generator performance report

Date:

## Scope

- Test project:
- Commit or branch:
- Profiler:
- Build command:

## Baseline

- Runs:
- Average:
- Top profiler entries:

## Change

- Files changed:
- Reason:

## Result

- Runs:
- Average:
- Difference:
- Top profiler entries after change:

## Verification

- Commands:
- Result:

## Next candidates

- Candidate hotspot:
- Why it looks expensive:
- Possible optimization:
```

Good performance work leaves behind both the code change and enough evidence for the next person to decide whether the win was real.
