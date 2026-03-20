namespace Pure.DI.Core;

using System.Globalization;

interface IGlobalProperties
{
    int MaxVariations { get; }

    int MaxDependencies { get; }

    int MaxMermaid { get; }

    CultureInfo? Culture { get; }

    bool TryGetProfilePath(out string path);
}