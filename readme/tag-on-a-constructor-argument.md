#### Tag on a constructor argument

`Tag.OnConstructorArg<T>(name)` creates a tag that targets the constructor parameter `name` of type `T`, letting you choose a specific implementation for a single injection site without adding `[Tag(...)]` attributes to the consuming class — useful when you can't or don't want to modify its code. The wildcards `*` and `?` are supported.


```c#
using Shouldly;
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind(Tag.OnConstructorArg<DataReplicator>("sourceStream"))
        .To<FileStream>()
    .Bind(Tag.OnConstructorArg<StreamProcessor<TT>>("stream"))
        .To<NetworkStream>()
    .Bind<IDataReplicator>().To<DataReplicator>()

    // Specifies to create the composition root named "Root"
    .Root<IDataReplicator>("Replicator");

var composition = new Composition();
var replicator = composition.Replicator;
replicator.SourceStream.ShouldBeOfType<FileStream>();
replicator.TargetStream.ShouldBeOfType<NetworkStream>();

interface IStream;

class FileStream : IStream;

class NetworkStream : IStream;

class StreamProcessor<T>(IStream stream)
{
    public IStream Stream { get; } = stream;
}

interface IDataReplicator
{
    IStream SourceStream { get; }

    IStream TargetStream { get; }
}

class DataReplicator(
    IStream sourceStream,
    StreamProcessor<string> processor)
    : IDataReplicator
{
    public IStream SourceStream { get; } = sourceStream;

    public IStream TargetStream => processor.Stream;
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

>[!WARNING]
>Each potentially injectable argument, property, or field contains an additional tag. This tag can be used to specify what can be injected there. This will only work if the binding type and the tag match. So while this approach can be useful for specifying what to enter, it can be more expensive to maintain and less reliable, so it is recommended to use attributes like `[Tag(...)]` instead.

<details>
<summary>The following partial class will be generated</summary>

```c#
partial class Composition
{
  public IDataReplicator Replicator
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      return new DataReplicator(new FileStream(), new StreamProcessor<string>(new NetworkStream()));
    }
  }
}
```

</details>

Class diagram:

```mermaid
---
 config:
  class:
   hideEmptyMembersBox: true
---
classDiagram
	FileStream --|> IStream
	NetworkStream --|> IStream
	DataReplicator --|> IDataReplicator
	Composition ..> DataReplicator : IDataReplicator Replicator
	DataReplicator *-- FileStream : IStream
	DataReplicator *-- StreamProcessorᐸStringᐳ : StreamProcessorᐸStringᐳ
	StreamProcessorᐸStringᐳ *-- NetworkStream : IStream
	namespace Pure.DI.UsageTests.Advanced.TagOnConstructorArgScenario {
		class Composition {
		<<partial>>
		+IDataReplicator Replicator
		}
		class DataReplicator {
				<<class>>
			+DataReplicator(IStream sourceStream, StreamProcessorᐸStringᐳ processor)
		}
		class FileStream {
				<<class>>
			+FileStream()
		}
		class IDataReplicator {
			<<interface>>
		}
		class IStream {
			<<interface>>
		}
		class NetworkStream {
				<<class>>
			+NetworkStream()
		}
		class StreamProcessorᐸStringᐳ {
				<<class>>
			+StreamProcessor(IStream stream)
		}
	}
```

See also:

- [Tag on injection site](tag-on-injection-site.md)
- [Tag on a member](tag-on-a-member.md)
- [Tag on a method argument](tag-on-a-method-argument.md)

