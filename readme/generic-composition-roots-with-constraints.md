#### Generic composition roots with constraints

>[!IMPORTANT]
>``Resolve` methods cannot be used to resolve generic composition roots.


```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // Disable Resolve methods to keep the public API minimal
    .Hint(Hint.Resolve, "Off")
    .Bind().To<StreamSource<TTDisposable>>()
    .Bind().To<DataProcessor<TTDisposable, TTS>>()
    // Creates SpecializedDataProcessor manually,
    // just for the sake of example.
    // It treats 'bool' as the options type for specific boolean flags.
    .Bind("Specialized").To(ctx => {
        ctx.Inject(out IStreamSource<TTDisposable> source);
        return new SpecializedDataProcessor<TTDisposable>(source);
    })

    // Specifies to create a regular public method
    // to get a composition root of type DataProcessor<T, TOptions>
    // with the name "GetProcessor"
    .Root<IDataProcessor<TTDisposable, TTS>>("GetProcessor")

    // Specifies to create a regular public method
    // to get a composition root of type SpecializedDataProcessor<T>
    // with the name "GetSpecializedProcessor"
    // using the "Specialized" tag
    .Root<IDataProcessor<TTDisposable, bool>>("GetSpecializedProcessor", "Specialized");

var composition = new Composition();

// Creates a processor for a Stream with 'double' as options (e.g., threshold)
// processor = new DataProcessor<Stream, double>(new StreamSource<Stream>());
var processor = composition.GetProcessor<Stream, double>();

// Creates a specialized processor for a BinaryReader
// specializedProcessor = new SpecializedDataProcessor<BinaryReader>(new StreamSource<BinaryReader>());
var specializedProcessor = composition.GetSpecializedProcessor<BinaryReader>();

interface IStreamSource<T>
    where T : IDisposable;

class StreamSource<T> : IStreamSource<T>
    where T : IDisposable;

interface IDataProcessor<T, TOptions>
    where T : IDisposable
    where TOptions : struct;

class DataProcessor<T, TOptions>(IStreamSource<T> source) : IDataProcessor<T, TOptions>
    where T : IDisposable
    where TOptions : struct;

class SpecializedDataProcessor<T>(IStreamSource<T> source) : IDataProcessor<T, bool>
    where T : IDisposable;
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
- Add a reference to the NuGet package
  - [Pure.DI](https://www.nuget.org/packages/Pure.DI)
```bash
dotnet add package Pure.DI
```
- Copy the example code into the _Program.cs_ file

You are ready to run the example 🚀
```bash
dotnet run
```

</details>

>[!IMPORTANT]
>The method `Inject()`cannot be used outside of the binding setup.

The following partial class will be generated:

```c#
partial class Composition
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IDataProcessor<T2, bool> GetSpecializedProcessor<T2>()
    where T2: IDisposable
  {
    SpecializedDataProcessor<T2> transientSpecializedDataProcessor512;
    IStreamSource<T2> localSource = new StreamSource<T2>();
    transientSpecializedDataProcessor512 = new SpecializedDataProcessor<T2>(localSource);
    return transientSpecializedDataProcessor512;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IDataProcessor<T2, T3> GetProcessor<T2, T3>()
    where T2: IDisposable
    where T3: struct
  {
    return new DataProcessor<T2, T3>(new StreamSource<T2>());
  }
}
```

Class diagram:

```mermaid
---
 config:
  maxTextSize: 2147483647
  maxEdges: 2147483647
  class:
   hideEmptyMembersBox: true
---
classDiagram
	SpecializedDataProcessorᐸT2ᐳ --|> IDataProcessorᐸT2ˏBooleanᐳ : "Specialized" 
	DataProcessorᐸT2ˏT3ᐳ --|> IDataProcessorᐸT2ˏT3ᐳ
	StreamSourceᐸT2ᐳ --|> IStreamSourceᐸT2ᐳ
	Composition ..> SpecializedDataProcessorᐸT2ᐳ : IDataProcessorᐸT2ˏBooleanᐳ GetSpecializedProcessorᐸT2ᐳ()
	Composition ..> DataProcessorᐸT2ˏT3ᐳ : IDataProcessorᐸT2ˏT3ᐳ GetProcessorᐸT2ˏT3ᐳ()
	SpecializedDataProcessorᐸT2ᐳ *--  StreamSourceᐸT2ᐳ : IStreamSourceᐸT2ᐳ
	DataProcessorᐸT2ˏT3ᐳ *--  StreamSourceᐸT2ᐳ : IStreamSourceᐸT2ᐳ
	namespace Pure.DI.UsageTests.Generics.GenericCompositionRootsWithConstraintsScenario {
		class Composition {
		<<partial>>
		+IDataProcessorᐸT2ˏT3ᐳ GetProcessorᐸT2ˏT3ᐳ()
		+IDataProcessorᐸT2ˏBooleanᐳ GetSpecializedProcessorᐸT2ᐳ()
		}
		class DataProcessorᐸT2ˏT3ᐳ {
				<<class>>
			+DataProcessor(IStreamSourceᐸT2ᐳ source)
		}
		class IDataProcessorᐸT2ˏBooleanᐳ {
			<<interface>>
		}
		class IDataProcessorᐸT2ˏT3ᐳ {
			<<interface>>
		}
		class IStreamSourceᐸT2ᐳ {
			<<interface>>
		}
		class SpecializedDataProcessorᐸT2ᐳ {
				<<class>>
		}
		class StreamSourceᐸT2ᐳ {
				<<class>>
			+StreamSource()
		}
	}
```

