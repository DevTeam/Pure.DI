namespace Pure.DI.UsageTests.Unity;

public class CreateAssetMenuAttribute: Attribute
{
    public string? fileName { get; set; }

    public string? menuName{ get; set; }
}