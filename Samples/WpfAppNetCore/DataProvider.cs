// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable 8604
namespace WpfAppNetCore;

using System.Windows;
using System.Windows.Data;

public class DataProvider : ObjectDataProvider
{
    public object? Tag { get; set; }

    protected override void BeginQuery() => OnQueryFinished(
        Application.Current is App
            ? ClockDomain.Resolve(ObjectType, Tag) // Running mode
            : ClockDomainDesignTime.Resolve(ObjectType, Tag)); // Design-time mode
}