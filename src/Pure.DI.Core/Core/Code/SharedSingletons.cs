// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

sealed class SharedSingletons(IConstructors constructors)
    : ISharedSingletons
{
    public bool IsShared(CompositionCode composition, VarDeclaration field) =>
        field.Node.ActualLifetime == Lifetime.Singleton && constructors.IsEnabled(composition.Source);

    public bool HasShared(CompositionCode composition) =>
        composition.Singletons.Any(field => IsShared(composition, field));

    public string GetOrCreateExpression(string owner) =>
        $"{owner}{Names.SingletonsFieldName} ?? {Names.SystemNamespace}Threading.Interlocked.CompareExchange(ref {owner}{Names.SingletonsFieldName}, new {Names.SingletonsClassName}(), null) ?? {owner}{Names.SingletonsFieldName}";
}
