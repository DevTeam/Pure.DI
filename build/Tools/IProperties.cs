namespace Build.Tools;

internal interface IProperties
{
    string this[string name] { get; }
}