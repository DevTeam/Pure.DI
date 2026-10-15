# Lifetimes and ownership

- `Transient` is the default: create a new instance for each injection.
- `Singleton` shares one instance per composition. Its dependencies and mutable state may be shared between threads.
- `Scoped` shares an instance within a scope. Use the project's scope pattern and dispose the scope at the end of the operation.
- `PerResolve` shares within one root resolution; `PerBlock` shares within a generated construction block. Check the scenario before choosing these less common lifetimes.

Choose a lifetime from the consumer's ownership and reuse requirements. Do not change one merely to silence a diagnostic. For disposable dependencies created through deferred factories, examine `Owned<T>` and the relevant disposal scenario before deciding who releases them.

Reference scenarios: https://github.com/DevTeam/Pure.DI/tree/master/readme . Build the project to verify the generated graph.
