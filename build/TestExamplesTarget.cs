// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ClassNeverInstantiated.Global

namespace Build;

class TestExamplesTarget(
    Commands commands,
    Env env,
    [Tag(typeof(CreateExamplesTarget))] ITarget<IReadOnlyCollection<ExampleGroup>> createExamplesTarget)
    : IInitializable, ITarget<int>
{
    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Test examples", "testexamples", "te");

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        new DotNetBuild().Run().EnsureSuccess();
        var examples = await createExamplesTarget.RunAsync(cancellationToken);
        var solutionDir = env.GetPath(PathType.SolutionDirectory);
        var tempDir = Path.Combine(env.GetPath(PathType.TempDirectory));
        Directory.CreateDirectory(tempDir);
        try
        {
            new DotNetNew()
                .WithWorkingDirectory(tempDir)
                .WithTemplateName("console")
                .WithName("App")
                .Run().EnsureSuccess();

            var appDir = Path.Combine(tempDir, "App");
            File.Copy(Path.Combine(solutionDir, "tests", "Pure.DI.UsageTests", "Test.props"), Path.Combine(appDir, "Directory.Build.props"));
            var programFile = Path.Combine(appDir, "Program.cs");
            foreach (var (_, groupExamples) in examples)
            {
                foreach (var vars in groupExamples)
                {
                    if (!HasIntegrationTest(vars))
                    {
                        continue;
                    }

                    var description = vars[CreateExamplesTarget.DescriptionKey];
                    var code = vars[CreateExamplesTarget.BodyKey];
                    await File.WriteAllTextAsync(programFile, code, cancellationToken);
                    var result = new DotNetBuild()
                        .WithShortName(description)
                        .WithProject(Path.Combine(appDir, "App.csproj"))
                        .WithWorkingDirectory(solutionDir)
                        .WithProps(("SolutionDir", solutionDir))
                        .Build();

                    if (result.ExitCode != 0)
                    {
                        WriteLine($"Test \"{description}\"", Color.Header);
                        foreach (var line in code.Split([Environment.NewLine], StringSplitOptions.None))
                        {
                            WriteLine(line, Color.Details);
                        }
                    }
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        return 0;
    }

    private static bool HasIntegrationTest(Example example) =>
        !example.TryGetValue(CreateExamplesTarget.IntegrationTestKey, out var integrationTestStr)
        || bool.TryParse(integrationTestStr, out var integrationTest) && integrationTest;
}