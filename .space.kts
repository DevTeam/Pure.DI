job("Build and check") {
    container(displayName = "Build and check", image = "nikolayp/dotnetsdk") {
        shellScript {
            content = "dotnet run --project ./build -- check"
        }
    }
}