// Run this script from the working directory where a target solution or project is located.

// Changes a verbosity level (Quiet, Normal, or Diagnostic).
// #l Diagnostic

// Adds a NuGet package and references to assemblies.
// #r "nuget: MyPackage, 1.2.3"

// Adds an assembly reference.
// #r "MyAssembly.dll"

// Includes code from a file in the order it should run.
// #load "MyClass.cs"

// Please see the page below for more details.
// https://github.com/JetBrains/teamcity-csharp-interactive

#load "Tools.cs"
#load "Settings.cs"
#load "Build.cs"
#load "Deploy.cs"
#load "DeployTemplate.cs"
#load "Benchmark.cs"
#load "Root.cs"
#load "Program.cs"
