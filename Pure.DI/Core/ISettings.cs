namespace Pure.DI.Core
{
    internal interface ISettings
    {
        bool TryGetOutputPath(out string outputPath);
    }
}
