// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Pure.DI.UsageTests.Unity;

[AttributeUsage(AttributeTargets.Class)]
public class CreateAssetMenuAttribute: Attribute
{
    public string? FileName { get; set; }

    public string? MenuName{ get; set; }
}