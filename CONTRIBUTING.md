## How to contribute to _Pure.DI_

Thank you for your interest in contributing to the _Pure.DI_ project! First of all, if you are going to make a big change or feature, please open a problem first. That way, we can coordinate and understand if the change you're going to work on fits with current priorities and if we can commit to reviewing and merging it within a reasonable timeframe. We don't want you to waste a lot of your valuable time on something that may not align with what we want for _Pure.DI_.

Contribution prerequisites: [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed.

The entire build logic is a regular [console .NET application](/build). You can use the [build.cmd](/build.cmd) and [build.sh](/build.sh) files with the appropriate command in the parameters to perform all basic actions on the project, e.g:

| Commands | Description |
|----------|-------------|
| bm, benchmarks, benchmarks | Runs benchmarks |
| c, check, check | Compatibility checks |
| dp, deploy, deploy | Deploys packages |
| e, example, example | Create examples |
| g, generator, generator | Builds and tests generator |
| i, install, install | Install template |
| l, libs, libs | Builds and tests libraries |
| p, pack, pack | Creates NuGet packages |
| perf, performance, performance | Runs performance tests |
| pb, publish, publish | Publish balazor web sssembly example |
| r, readme, readme | Generates README.md |
| t, template, template | Creates and deploys template |
| te, testexamples, testexamples | Test examples |
| u, upgrade, upgrade | Upgrading the internal version of DI to the latest public version |

For example:

```shell
./build.sh pack
```

or

```shell
./build.cmd benchmarks
```

If you are using the Rider IDE, it already has a set of configurations to run these commands. This project uses [C# interactive](https://github.com/DevTeam/csharp-interactive) build automation system for .NET. This tool helps to make .NET builds more efficient.

![](https://raw.githubusercontent.com/DevTeam/csharp-interactive/master/docs/CSharpInteractive.gif)

Thanks!
