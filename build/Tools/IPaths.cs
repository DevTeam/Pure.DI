namespace Build.Tools;

internal interface IPaths
{
    string SolutionDirectory { get; }

    string TempDirectory { get; }
}