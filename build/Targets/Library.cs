namespace Build.Targets;

internal record Library(
    string Name,
    Package Package,
    string[] Frameworks,
    string[] TemplateNames);