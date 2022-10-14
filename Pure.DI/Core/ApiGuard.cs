// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;
using NS35EBD81B;

internal class ApiGuard : IApiGuard
{
    public bool IsAvailable(Compilation compilation)
    {
        var diType = compilation.GetTypesByMetadataName(typeof(DI).FullName.ReplaceNamespace()).FirstOrDefault();
        if (diType == null)
        {
            return false;
        }

        var type = (
            from tree in compilation.SyntaxTrees
            let semanticModel = compilation.GetSemanticModel(tree)
            from typeDeclaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            let symbol = ModelExtensions.GetDeclaredSymbol(semanticModel, typeDeclaration)
            select symbol).FirstOrDefault();

        return type != null && compilation.IsSymbolAccessibleWithin(diType, type);
    }
}