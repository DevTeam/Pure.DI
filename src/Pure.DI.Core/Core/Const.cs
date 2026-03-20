namespace Pure.DI.Core;

static class Const
{
    public const string SeverityProperty = "build_property.purediseverity";
    public const string LogFileProperty = "build_property.puredilogfile";
    public const string ProfilePathProperty = "build_property.purediprofilepath";
    public const string CultureProperty = "build_property.puredipculture";

    public const int MaxStackalloc = 32;

    public const string MaxDependenciesProperty = "build_property.puredimaxdependencies";
    public const int MinDependencies = 256;
    public const int DefaultDependencies = 32768;

    public const string MaxVariationsProperty = "build_property.puredimaxvariations";
    public const int MinVariations = 256;
    public const int DefaultVariations = 16384;

    public const string MaxMermaidProperty = "build_property.puredimaxmermaid";
    public const int MinMermaid = 0;
    public const int DefaultMermaid = 64;
}