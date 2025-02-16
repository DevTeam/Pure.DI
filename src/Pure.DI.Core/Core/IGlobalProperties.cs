namespace Pure.DI.Core;

internal interface IGlobalProperties
{
    int MaxIterations { get; }

    bool TryGetProfilePath(out string path);
}