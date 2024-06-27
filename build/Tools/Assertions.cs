namespace Build.Tools;

internal static class Assertions
{
    public static void Succeed(this int? exitCode, string shortName)
    {
        if (exitCode == 0)
        {
            return;
        }

        Error($"{shortName} failed.");
        Environment.Exit(1);
    }

    public static void Succeed(this IBuildResult result)
    {
        if (result.ExitCode == 0)
        {
            return;
        }
        
        var failedTests = 
            from testResult in result.Tests
            where testResult.State == TestState.Failed
            select testResult;

        foreach (var failedTest in failedTests)
        {
            Error(failedTest.ResultDisplayName);
        }

        Error($"{result.StartInfo.ShortName} failed");
        throw new OperationCanceledException();
    }
}