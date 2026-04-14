#### Scope setup method


```c#
using Shouldly;
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
IRequestContext ctx1;
IRequestContext ctx2;

// Request #1
using (var request1 = Composition.SetupScope(composition, new Composition()))
{
    var checkout11 = request1.RequestRoot;
    var checkout12 = request1.RequestRoot;
    ctx1 = checkout11.Context;

    // Same request => same scoped instance
    ctx1.ShouldBe(checkout12.Context);
    ctx1.IsDisposed.ShouldBeFalse();
}

// End of request #1 => scoped instance is disposed
ctx1.IsDisposed.ShouldBeTrue();

// Request #2
using (var request2 = Composition.SetupScope(composition, new Composition()))
{
    var checkout2 = request2.RequestRoot;
    ctx2 = checkout2.Context;
}

// Different request => different scoped instance
ctx1.ShouldNotBe(ctx2);

// End of request #2 => scoped instance is disposed
ctx2.IsDisposed.ShouldBeTrue();

interface IIdGenerator
{
    Guid Generate();
}

class IdGenerator : IIdGenerator
{
    public Guid Generate() => Guid.NewGuid();
}

interface IRequestContext
{
    Guid CorrelationId { get; }

    bool IsDisposed { get; }
}

// Typically: DbContext / UnitOfWork / RequestTelemetry / Activity, etc.
sealed class RequestContext(IIdGenerator idGenerator)
    : IRequestContext, IDisposable
{
    public Guid CorrelationId { get; } = idGenerator.Generate();

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface ICheckoutService
{
    IRequestContext Context { get; }
}

// "Controller/service" that participates in request processing.
// It depends on a scoped context (per-request resource).
sealed class CheckoutService(IRequestContext context)
    : ICheckoutService
{
    public IRequestContext Context => context;
}

partial class Composition
{
    static void Setup() =>

        DI.Setup()
            .Hint(Hint.ScopeMethodName, "SetupScope")
            // Per-request lifetime
            .Bind().As(Scoped).To<RequestContext>()

            .Bind().As(Singleton).To<IdGenerator>()

            // Regular service that consumes scoped context
            .Bind().To<CheckoutService>()

            // "Request root" (what your controller/handler resolves)
            .Root<ICheckoutService>("RequestRoot");
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later installed
```bash
dotnet --list-sdk
```
- Create a net10.0 (or later) console application
```bash
dotnet new console -n Sample
```
- Add references to the NuGet packages
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
  - [Shouldly](https://www.nuget.org/packages/Shouldly)
```bash
dotnet add package Pure.DI
dotnet add package Shouldly
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>



