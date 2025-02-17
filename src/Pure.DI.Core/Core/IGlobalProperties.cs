namespace Pure.DI.Core;

interface IGlobalProperties
{
    int MaxIterations { get; }

    bool TryGetProfilePath(out string path);
}