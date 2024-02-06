namespace Build.Targets;

internal interface IInitializable
{
    Task InitializeAsync();
}