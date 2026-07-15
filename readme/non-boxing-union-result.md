#### Non-boxing union result

The compact `union` declaration stores its value as `object`, which is convenient but boxes value-type cases. On a hot path, a custom union can keep each value-type case in a dedicated field and expose the optional `HasValue`/`TryGetValue` pattern. Pure.DI still treats the selected case as the union DI contract through the compiler's implicit union conversion; no reflection, runtime lookup, or wrapper allocation is introduced by the composition.
This cache lookup example binds the value-type `CacheHit` case to the `CacheLookupResult` contract. The generated root constructs `CacheHit` directly and the C# compiler invokes the union creation constructor. The consumer reads it with `TryGetValue(out CacheHit)`, so the normal success path does not access the object-typed `Value` fallback and does not box the case.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    // CacheHit is a value-type case of CacheLookupResult
    .Bind<CacheLookupResult>().To<CacheHit>()
    .Bind<int>("product id").To(() => 42)
    .Bind<decimal>("price").To(() => 19.95m)
    .Root<CacheLookupResult>("CachedProduct");

var result = new Composition().CachedProduct;

result.TryGetValue(out CacheHit hit).ShouldBeTrue();
hit.ProductId.ShouldBe(42);
hit.Price.ShouldBe(19.95m);

readonly record struct CacheHit(
    [Tag("product id")] int ProductId,
    [Tag("price")] decimal Price);

readonly record struct CacheMiss(int ProductId);

// A custom union stores value-type cases directly. Value is the required
// general fallback; TryGetValue is the allocation-free access path.
[System.Runtime.CompilerServices.Union]
readonly struct CacheLookupResult : System.Runtime.CompilerServices.IUnion
{
    private readonly byte _kind;
    private readonly CacheHit _hit;
    private readonly CacheMiss _miss;

    public CacheLookupResult(CacheHit hit) =>
        (_kind, _hit, _miss) = (1, hit, default);

    public CacheLookupResult(CacheMiss miss) =>
        (_kind, _hit, _miss) = (2, default, miss);

    public object? Value => _kind switch
    {
        1 => _hit,
        2 => _miss,
        _ => null
    };

    public bool HasValue => _kind != 0;

    public bool TryGetValue(out CacheHit value)
    {
        value = _hit;
        return _kind == 1;
    }

    public bool TryGetValue(out CacheMiss value)
    {
        value = _miss;
        return _kind == 2;
    }
}
```

<details>
<summary>Running this code sample locally</summary>

- Make sure you have the [.NET SDK 11.0](https://dotnet.microsoft.com/en-us/download/dotnet/11.0) or later installed
```bash
dotnet --list-sdk
```
- Create a net11.0 (or later) console application
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

>[!NOTE]
>The `Value` property is mandatory for a custom union and necessarily boxes value-type cases when it is read. Use the strongly typed `TryGetValue` members on performance-sensitive paths; reserve `Value` for diagnostics and general-purpose inspection.
This scenario requires the preview language version and .NET 11 Preview 5 or later, where the union runtime types are available.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  public CacheLookupResult CachedProduct
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      int transientInt32 = 42;
      decimal transientDecimal = 19.95m;
      return new CacheHit(transientInt32, transientDecimal);
    }
  }
}
```

</details>


