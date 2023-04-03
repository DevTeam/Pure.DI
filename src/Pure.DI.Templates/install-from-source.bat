dotnet pack -c Release Pure.DI.Templates.csproj
dotnet new -u Pure.DI.Templates
dotnet new -i Pure.DI.Templates::1.0.0 --nuget-source %cd%/bin