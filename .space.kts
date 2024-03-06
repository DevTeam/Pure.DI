job("Build and check") {
    container(displayName = "Build and check", image = "mcr.microsoft.com/dotnet/sdk") {
        shellScript {
            content = "dotnet run --project ./build -- check"
        }
    }
}