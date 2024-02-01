namespace Build.Targets;

internal interface IInitializable
{
    ValueTask InitializeAsync();
}