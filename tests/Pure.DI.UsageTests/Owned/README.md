# `Owned<T>` behaviour comparison suite

These tests pin Pure.DI's `Owned<T>` disposal and lifetime behaviour against
**Autofac**'s `Owned<T>` (`Autofac.Features.OwnedInstances.Owned<T>`), used as a
reference implementation. Every test asserts *both* containers against the *same*
expected value, so the expected literals are grounded in a mature container's
observed behaviour rather than guessed.

## How the Autofac oracle is wired

To keep the committed suite free of any Autofac dependency, all Autofac code is
compiled only under the `AUTOFAC_REFERENCE` symbol. Without it — the default, and
what CI sees — the suite builds and runs as pure Pure.DI tests (no Autofac
package, no reference).

To turn the Autofac side on locally, create
`tests/Pure.DI.UsageTests/Pure.DI.UsageTests.csproj.user` (git-ignored):

```xml
<Project>
  <PropertyGroup>
    <DefineConstants>$(DefineConstants);AUTOFAC_REFERENCE</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Autofac" Version="8.*" />
  </ItemGroup>
</Project>
```

MSBuild auto-imports `*.csproj.user`, so no other change is needed. Run just this
suite with:

```shell
dotnet test tests/Pure.DI.UsageTests/Pure.DI.UsageTests.csproj --filter "FullyQualifiedName~Owned."
```

With or without `AUTOFAC_REFERENCE`, the same 5 tests fail — the Autofac side
only adds a second, independent witness that the expected values are correct.

## Results

**23 of 28 tests confirm parity** with Autofac: transient/singleton/scoped
disposal, root isolation, owned-of-singleton lifetime, mixed-lifetime nested
graphs, diamond (shared) dependencies, LIFO disposal order, factory-created
disposables, all async-disposal paths (including sync-over-async and mixed
sync/async), double-dispose idempotency, and explicit release by a singleton
owner.

**5 tests fail on purpose**, documenting divergences from Autofac:

| Failing test | Divergence |
| --- | --- |
| `MessagePumpTests` | A captured `Func<Owned<T>>` reuses one accumulator per resolution, so in the canonical message-pump loop only the first unit of work is disposed; every later one leaks. |
| `NestedOwnedTests` (×2) | Outer and inner `Owned<T>` share that same accumulator, so disposing either the outer or the inner disposes *both* graphs. |
| `DisposalExceptionTests` | `Owned<T>.Dispose()` swallows exceptions thrown by a component's `Dispose()` (routed to the empty `OnDisposeException` partial); Autofac surfaces them. |
| `SingletonOwnerTests` | A never-released `Owned<T>` is not disposed on composition disposal; Autofac disposes it as a container-level safety net. |

The first three rows share one root cause: the generated composition allocates a
single `Pure.DI.Owned` accumulator per composition-root resolution and reuses it
for every `Owned<T>` created in that block, rather than giving each `Owned<T>`
its own. Autofac gives each `Owned<T>` an independent lifetime scope.
