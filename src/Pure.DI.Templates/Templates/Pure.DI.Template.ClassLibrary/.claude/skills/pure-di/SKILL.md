---
name: pure-di
description: Implement or fix dependency injection with Pure.DI in a .NET project, including bindings, roots, lifetimes, tags, factories, scopes, and Pure.DI generator diagnostics. Use when the project references Pure.DI or the user asks to add it.
---

# Work with Pure.DI

Inspect the project's `PackageReference` and existing `DI.Setup` before changing its composition. Preserve the project's Pure.DI version and established composition style. Pure.DI generates object creation code at compile time; generated members may not exist in source files before a build.

For ordinary bindings and roots, read [bindings and roots](references/bindings.md). Read [lifetimes](references/lifetimes.md) when changing sharing, scopes, factories, or disposal. Read [diagnostics](references/diagnostics.md) when compilation reports a Pure.DI diagnostic.

Implement the smallest graph change that satisfies the request. Run `dotnet build` for the affected project. If it fails, address the first relevant C# or Pure.DI diagnostic and rebuild. Do not suppress diagnostics to make a broken graph compile.
