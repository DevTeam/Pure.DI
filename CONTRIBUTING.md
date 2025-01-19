## How to contribute to _Pure.DI_

Thank you for your interest in contributing to the _Pure.DI_ project! First of all, if you are going to make a big change or feature, please open a problem first. That way, we can coordinate and understand if the change you're going to work on fits with current priorities and if we can commit to reviewing and merging it within a reasonable timeframe. We don't want you to waste a lot of your valuable time on something that may not align with what we want for _Pure.DI_.

Contribution prerequisites: [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later is installed.

This repository contains the following directories and files:

```
📁 .github                       - GitHub related files and main.yml for building using GitGub actions
📁 .logs                         - temporary files for generating the README.md file
📁 .run                          - configuration files for the Rider IDE
📁 benchmarks                    - projects for performance measurement
📁 build                         - application for building locally and using CI/CD
📁 docs                          - resources for the README.md file
📁 readme                        - sample scripts and examples of application implementations
📁 samples                       - sample project
📁 src                           - source codes of the code generator and all libraries
|- 📂 Pure.DI                    - source code generator project
|- 📂 Pure.DI.Abstractions       - abstraction library for Pure.DI
|- 📂 Pure.DI.Core               - basic implementation of the source code generator
|- 📂 Pure.DI.MS                 - project for integration with Microsoft DI
|- 📂 Pure.DI.Templates          - project templates for creating .NET projects using Pure.DI
|- 📄 Directory.Build.props      - common MSBUILD properties for all source code generator projects
|- 📄 Library.props              - common MSBUILD properties for library projects such as Pure.DI.Abstractions
📁 tests                         - contains projects for testing
|- 📂 Pure.DI.Example            - project for testing some integration scenarios
|- 📂 Pure.DI.IntegrationTests   - integration tests
|- 📂 Pure.DI.Tests              - unit tests for basic functionality
|- 📂 Pure.DI.UsageTests         - usage tests, used for examples in README.md
|- 📄 Directory.Build.props      - common MSBUILD properties for all test projects
📄 LICENSE                       - license file
📄 build.cmd                     - Windows script file to run one of the build steps, see description below
📄 build.sh                      - Linux/Mac OS script file to run one of the build steps, see description below
📄 .space.kts                    - build file using JetBrains space actions
📄 README.md                     - this README.md file
📄 SECURITY.md                   - policy file for handling security bugs and vulnerabilities
📄 Directory.Build.props         - basic MSBUILD properties for all projects
📄 Pure.DI.sln                   - .NET solution file
|- ...
```

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
