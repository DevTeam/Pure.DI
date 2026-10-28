# Working on Pure.DI

Pure.DI is a compile-time dependency injection source generator. Changes to its API or generated code should be checked against the relevant scenarios in `tests/Pure.DI.UsageTests` and the diagnostic reference in `DIAGNOSTICS.md`.

The consumer-facing agent guidance lives in `agent-context/`. Both project templates carry copies of that guidance. Keep those copies synchronized when editing the skill or its references. The root `AGENTS.md` is for contributors to this repository; it is not shipped as consumer guidance.

Use `dotnet run --project ./build -- check` for the repository validation workflow. For a focused change, build and test the affected project first.

To see how a change affects the generated code, run `tests/Pure.DI.IntegrationTests` before and after it with the `PUREDI_DUMP_DIR` environment variable set to a different directory each time, then compare the directories. Each test writes its generated code to a file named after its sources. For the usage scenarios, build `tests/Pure.DI.UsageTests` with `-p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=<directory>`.
