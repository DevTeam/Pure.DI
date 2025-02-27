// ReSharper disable StringLiteralTypo
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

class InstallTemplateTarget(
    Commands commands,
    [Tag(typeof(TemplateTarget))] ITarget<Package> templateTarget)
    : IInitializable, ITarget<Package>
{
    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Install templates", "install", "i");

    public async Task<Package> RunAsync(CancellationToken cancellationToken)
    {
        var package = await templateTarget.RunAsync(cancellationToken);
        await new DotNetNewUninstall()
            .WithPackage(TemplateTarget.ProjectName)
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

        await new DotNetNewInstall()
            .WithPackage($"{TemplateTarget.ProjectName}::{package.Version}")
            .AddSources(Path.GetFullPath(Path.GetDirectoryName(package.Path) ?? "."))
            .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

        return package;
    }
}