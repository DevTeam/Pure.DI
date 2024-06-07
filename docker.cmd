docker.exe pull nikolayp/dotnetsdk:latest
docker.exe run --rm -it -v D:\Projects\Pure.DI:/src -w /src nikolayp/dotnetsdk:latest dotnet build