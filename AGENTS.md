# Working on Pure.DI

Pure.DI is a compile-time dependency injection source generator. Changes to its API or generated code should be checked against the relevant scenarios in `tests/Pure.DI.UsageTests` and the diagnostic reference in `DIAGNOSTICS.md`.

The consumer-facing agent guidance lives in `agent-context/`. Both project templates carry copies of that guidance. Keep those copies synchronized when editing the skill or its references. The root `AGENTS.md` is for contributors to this repository; it is not shipped as consumer guidance.

Use `dotnet run --project ./build -- check` for the repository validation workflow. For a focused change, build and test the affected project first.

To see how a change affects the generated code, run `tests/Pure.DI.IntegrationTests` before and after it with the `PUREDI_DUMP_DIR` environment variable set to a different directory each time, then compare the directories. Each test writes its generated code to a file named after its sources. For the usage scenarios, build `tests/Pure.DI.UsageTests` with `-p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=<directory>`.

## Code style rules for reviewers and agents

- Do not convert `foreach`/`for` loops to LINQ. The generator runs inside the compiler, so avoiding enumerator, closure and delegate allocations matters more than brevity. The inspections that suggest such a conversion (`LoopCanBeConvertedToQuery`, `LoopCanBePartlyConvertedToQuery`, `ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator`, `ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator`) are switched off in `Pure.DI.sln.DotSettings`, and such findings are not problems to fix.
- When the code intentionally keeps a loop or an allocation-free path, prefer a short comment explaining why over a suppression attribute.
- Ordering that affects generated code, diagnostics or documentation must not depend on the culture of the machine: use `StringComparer.Ordinal` for identifiers and syntax names, and `StringComparer.InvariantCulture` where the order is meant to read like words. `StringComparer.Ordinal` is not a drop-in replacement for the default comparer: it puts `Sample.Type` before `int(99)`, so changing it can reorder expected output and must be checked against the scenarios.
- A suggestion from Inspect Code is a problem to fix only when it names a real defect, dead code, a culture dependency or a resource leak. Suggestions that only restate a deliberate choice are not problems: switch the inspection off in `Pure.DI.sln.DotSettings`, or add a file-level `// ReSharper disable` with a comment that says why, instead of rewriting the code.

