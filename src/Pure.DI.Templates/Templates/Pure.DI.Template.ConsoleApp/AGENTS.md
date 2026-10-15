<!-- Pure.DI agent instructions: begin -->
## Pure.DI in this project

This project uses the Pure.DI compile-time dependency injection generator. When changing a composition, binding, root, lifetime, tag, or factory, use the `pure-di` skill and follow the Pure.DI version referenced by the project.

Prefer explicit bindings for abstractions in application code. Add a named `Root<T>` for each intended entry point; do not replace the composition with a runtime service locator. After changing the graph, run `dotnet build` and fix the first Pure.DI diagnostic before continuing.

Project-specific instructions in this repository still apply.
<!-- Pure.DI agent instructions: end -->
