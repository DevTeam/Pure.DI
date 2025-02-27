namespace Build.Core;

record Library(
    string Name,
    Package Package,
    string[] Frameworks,
    string[] TemplateNames);