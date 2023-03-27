namespace WpfAppNetCore;

using System.Windows;
using System.Windows.Data;

public class DataProvider : ObjectDataProvider
{
    private readonly bool _isInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
    private readonly Composition _composite = new();
    private readonly DesignTimeComposition _designTimeComposite = new();

    public object? Tag { get; set; }

    protected override void BeginQuery() => OnQueryFinished(
        _isInDesignMode
            ? _designTimeComposite.Resolve(ObjectType, Tag)
            : _composite.Resolve(ObjectType, Tag));
}