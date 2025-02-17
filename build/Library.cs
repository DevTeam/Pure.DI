namespace Build;

record Library(
    string Name,
    Package Package,
    string[] Frameworks,
    string[] TemplateNames);