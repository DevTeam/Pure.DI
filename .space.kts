job("Pack") {
    container(displayName = "Say Hello", image = "mcr.microsoft.com/dotnet/sdk") {
        shellScript {
            content = "dotnet run --project ./build -- pack"
        }
    }
}