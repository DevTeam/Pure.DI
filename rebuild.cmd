for /d /r %%i in (obj) do @rmdir /S /Q "%%i"
for /d /r %%i in (bin) do @rmdir /S /Q "%%i"
dotnet build-server shutdown
dotnet build