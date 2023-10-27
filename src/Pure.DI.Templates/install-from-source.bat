dotnet pack -c Release Pure.DI.Templates.csproj -p:version=2.0.0
dotnet new uninstall Pure.DI.Templates
dotnet new install Pure.DI.Templates::2.0.0 --nuget-source %cd%/bin