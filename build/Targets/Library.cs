namespace Build.Targets;

internal record Library(
    string Name,
    string PackagePath,
    string[] Frameworks,
    string[] TemplateNames);