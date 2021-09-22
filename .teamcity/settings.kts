import jetbrains.buildServer.configs.kotlin.v2019_2.BuildType
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.*
import jetbrains.buildServer.configs.kotlin.v2019_2.project
import jetbrains.buildServer.configs.kotlin.v2019_2.triggers.*
import jetbrains.buildServer.configs.kotlin.v2019_2.vcs.GitVcsRoot
import jetbrains.buildServer.configs.kotlin.v2019_2.version

version = "2021.1"

// Build settings
open class Settings {
    companion object {
        const val getNextVersionScript =
            "using System.Linq;\n" +
            "Props[\"version\"] = \n" +
            "  GetService<INuGet>()\n" +
            "  .Restore(Args[0], \"*\", \"net5.0\")\n" +
            "  .Where(i => i.Name == Args[0])\n" +
            "  .Select(i => i.Version)\n" +
            "  .Select(i => new Version(i.Major, i.Minor, i.Build + 1))\n" +
            "  .DefaultIfEmpty(new Version(1, 0, 0))\n" +
            "  .Max()\n" +
            "  .ToString();\n" +
            "WriteLine($\"Version: {Props[\"version\"]}\", Success);"
    }
}

project {
    params {
        param("system.configuration", "Release")
        param("system.version", "1.0.0")
    }

    vcsRoot(Repo)
    buildType(BuildAndTestBuildType)
    buildType(DeployPureDIBuildType)
    buildType(DeployTemplateBuildType)
}

object Repo : GitVcsRoot({
    name = "Pure.DI"
    url = "https://github.com/DevTeam/Pure.DI.git"
    branch = "refs/heads/master"
})

object BuildAndTestBuildType: BuildType({
    name = "Build and test"
    vcs { root(Repo) }

    steps {
        dotnetTest {
            name = "Run tests"
        }
    }

    triggers {
        vcs {
        }
    }

    failureConditions {
        nonZeroExitCode = true
        testFailure = true
        errorMessage = true
    }
})

object DeployPureDIBuildType: BuildType({
    name = "Deploy Pure.DI"
    artifactRules = "%packagePath% => ."
    params {
        param("packageId", "Pure.DI")
        param("packagePath", "%packageId%/bin/%system.configuration%/%packageId%.%system.version%.nupkg")
    }
    vcs { root(Repo) }
    steps {
        csharpScript {
            name = "Evaluate a next NuGet package version"
            content = Settings.getNextVersionScript
            arguments = "%packageId%"
        }
        dotnetBuild {
            name = "Build"
            sdk = "5.0"
        }
        dotnetTest {
            name = "Run tests"
            skipBuild = true
        }
        dotnetPack {
            name = "Create a NuGet package"
            workingDir = "%packageId%"
            skipBuild = true
        }
        dotnetNugetPush {
            name = "Push the NuGet package"
            packages = "%packagePath"
            serverUrl = "https://api.nuget.org/v3/index.json"
            apiKey = "%NuGetKey%"
        }
    }

    failureConditions {
        nonZeroExitCode = true
        testFailure = true
        errorMessage = true
    }
})

object DeployTemplateBuildType: BuildType({
    name = "Deploy Template"
    artifactRules = "%packagePath% => ."
    params {
        param("packageId", "Pure.DI.Templates")
        param("packagePath", "%packageId%/bin/%system.configuration%/%packageId%.%system.version%.nupkg")
    }
    vcs { root(Repo) }
    steps {
        csharpScript {
            name = "Evaluate a next NuGet package version"
            content = Settings.getNextVersionScript
            arguments = "%packageId%"
        }
        dotnetPack {
            name = "Create a NuGet package"
            workingDir = "%packageId%"
            skipBuild = true
        }
        dotnetNugetPush {
            name = "Push the NuGet package"
            packages = "%packagePath"
            serverUrl = "https://api.nuget.org/v3/index.json"
            apiKey = "%NuGetKey%"
        }
    }

    failureConditions {
        nonZeroExitCode = true
        errorMessage = true
    }
})