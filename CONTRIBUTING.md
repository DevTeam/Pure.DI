## How to contribute to Pure.DI

Thank you for your interest in contributing to the Pure.DI project! If you are planning a big change or feature, please open an issue first. That way, we can coordinate and understand whether the change fits current priorities and whether we can commit to reviewing and merging it within a reasonable timeframe. We do not want you to spend your valuable time on something that may not align with the direction of Pure.DI.

Contribution prerequisites: [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later installed.

This repository contains the following directories and files:

```
📁 .github                       GitHub related files and main.yml for building using GitGub actions
📁 .logs                         temporary files for generating the README.md file
📁 .run                          configuration files for the Rider IDE
📁 benchmarks                    projects for performance measurement
📁 build                         application for building locally and using CI/CD
📁 docs                          resources for the README.md file
📁 readme                        sample scripts and examples of application implementations
📁 samples                       sample projects
📁 src                           source codes of the code generator and all libraries
|- 📂 Pure.DI                    source code generator project
|- 📂 Pure.DI.Abstractions       abstraction library for Pure.DI
|- 📂 Pure.DI.Core               basic implementation of the source code generator
|- 📂 Pure.DI.MS                 project for integration with Microsoft DI
|- 📂 Pure.DI.Templates          project templates for creating .NET projects using Pure.DI
|- 📄 Directory.Build.props      common MSBUILD properties for all source code generator projects
|- 📄 Library.props              common MSBUILD properties for library projects such as Pure.DI.Abstractions
📁 tests                         contains projects for testing
|- 📂 Pure.DI.Example            project for testing some integration scenarios
|- 📂 Pure.DI.IntegrationTests   integration tests
|- 📂 Pure.DI.Tests              unit tests for basic functionality
|- 📂 Pure.DI.UsageTests         usage tests, used for examples in README.md
|- 📄 Directory.Build.props      common MSBUILD properties for all test projects
📄 LICENSE                       license file
📄 build.cmd                     Windows script file to run one of the build steps, see description below
📄 build.sh                      Linux/Mac OS script file to run one of the build steps, see description below
📄 .space.kts                    build file using JetBrains space actions
📄 README.md                     this README.md file
📄 SECURITY.md                   policy file for handling security bugs and vulnerabilities
📄 Directory.Build.props         basic MSBUILD properties for all projects
📄 Pure.DI.slnx                  .NET solution file
```

The build logic is a regular [.NET console application](/build). You can use [build.cmd](/build.cmd) and [build.sh](/build.sh) with the appropriate command parameters to perform all basic actions on the project, for example:

| Commands | Description |
|----------|-------------|
|  | Create Pure.DI.Agent .NET tool package |
| bm | Run benchmarks |
| c | Compatibility checks |
| cgp, codegen-perf | Profile Pure.DI code generation |
| dp | Package deployment |
| e | Create examples |
| g | Build and test the source code generator |
| i | Install templates |
| l | Build and test libraries |
| p | Create NuGet packages |
| perf | Performance tests |
| pb | Publish the balazor web sssembly example |
| r | Generate README.md |
| rel | Get release information |
| t | Create and deploy templates |
| te | Test examples |
| u | Upgrading the internal version of DI to the latest public version |

For example, to build and test the source code generator: 

```shell
./build.sh generator
```

or to run benchmarks:

```shell
./build.cmd benchmarks
```

### Build and publish NuGet packages on GitHub

Create and push a version tag, such as `2.5.8` or `2.5.8-preview.1`, pointing to the release commit. Use canonical SemVer without a `v` prefix or build metadata. The tagged commit must contain the release build changes described here; old tags are not upgraded by the workflow.

Open **Actions → Publish NuGet packages → Run workflow** on the default branch. Enter the existing tag in **tag**. Leave **publish** unchecked to build and download packages without publishing, or check it to publish to NuGet.org. Pushing a tag or creating a GitHub release does not trigger publication.

The workflow checks out the tag's resolved commit, runs `pack` (including tests and compatibility checks), builds and smoke-tests templates against the local generator package, and smoke-tests the agent installer. All five packages (`Pure.DI`, `Pure.DI.MS`, `Pure.DI.Abstractions`, `Pure.DI.Agent`, and `Pure.DI.Templates`) use the tag version. Package IDs and versions are validated from their nuspec files. The run summary records the commit and package SHA256 hashes.

After a successful build, open the run's **Artifacts** section and download **nuget-packages**. The ZIP contains the five `.nupkg` files and is retained for 30 days, including runs with **publish** unchecked. Download it before retention expires if you need a longer-lived copy.

Publication is a separate job that downloads the same artifacts without rebuilding. Releases are serialized. Existing package versions are skipped, so after a partial publication you can use **Re-run failed jobs** to resume with the same artifacts. NuGet publication is not atomic; already published packages remain published if a later push fails. Skipping an existing version does not verify that its contents match the current artifact. Never move release tags or reuse a version for different contents.

Before the first publication:

1. Create the GitHub environment **nuget**. Set environment secret **NUGET_USER** to the NuGet.org profile name (not an email address). Restrict deployment to your default branch; add required reviewers only if you want a separate approval step.
2. On NuGet.org, create a [Trusted Publishing policy](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) for repository owner **DevTeam**, repository **Pure.DI**, workflow file **packages.yml**, and environment **nuget**. Select the package owner and scopes covering all five exact package IDs, including permission to create `Pure.DI.Agent` if it has not been published yet.
3. Merge the workflow and build changes into the default branch before creating the release tag. Run once with **publish** unchecked to inspect the artifacts, then run with **publish** checked when ready. A new run rebuilds; rerunning only a failed publication job reuses its original artifacts.

`NuGet/login@v1` obtains a short-lived key using GitHub OIDC just before publication. No long-lived NuGet API key is required. Build-only runs do not require the NuGet environment or secret.

Locally, use `dotnet run --project ./build -- "-p:version=2.5.8" pack`, then `dotnet run --project ./build -- "-p:version=2.5.8" "-p:templatePackageSource=<absolute path to .packages>" template` to build all five packages without publishing. Do not supply `NuGetKey` when building templates only.

If you are using the Rider IDE, it already has a set of configurations to run these commands. This project uses [C# interactive](https://github.com/DevTeam/csharp-interactive) build automation system for .NET. This tool helps to make .NET builds more efficient.

![](https://raw.githubusercontent.com/DevTeam/csharp-interactive/master/docs/CSharpInteractive.gif)

Thanks!
