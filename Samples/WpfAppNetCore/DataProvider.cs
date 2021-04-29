namespace WpfAppNetCore
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Data;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class DataProvider: ObjectDataProvider
    {
        public object Tag { get; set; }

        protected override void BeginQuery() => OnQueryFinished(
            Application.Current is App 
                ? ClockDomain.Resolve(ObjectType, Tag)              // Real-time
                : ClockDomainDesignTime.Resolve(ObjectType, Tag));  // Design-time
    }
}
