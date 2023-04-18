// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace WpfAppNetCore;

using System.Windows;
using System.Windows.Data;

public class DataProvider : ObjectDataProvider
{
    private static readonly bool IsInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
    private static readonly Composition Composite = new();
    private static readonly DesignTimeComposition DesignTimeComposite = new();

    public object? Tag { get; set; }

    protected override void BeginQuery() => OnQueryFinished(
        IsInDesignMode
            ? DesignTimeComposite.Resolve(ObjectType, Tag)
            : Composite.Resolve(ObjectType, Tag));
}