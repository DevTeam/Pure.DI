// ReSharper disable HeapView.DelegateAllocation

namespace Pure.DI.Core;

internal class Attributes(ISemantic semantic)
    : IAttributes
{
    public T GetAttribute<TMdAttribute, T>(
        SemanticModel semanticModel,
        in ImmutableArray<TMdAttribute> metadata,
        ISymbol member,
        T defaultValue)
        where TMdAttribute : IMdAttribute
    {
        if (metadata.IsDefaultOrEmpty)
        {
            return defaultValue;
        }

        foreach (var attributeMetadata in metadata)
        {
            var attributeData = GetAttributes(member, attributeMetadata.AttributeType);
            switch (attributeData.Count)
            {
                case 1:
                    var attr = attributeData[0];
                    if (typeof(ITypeSymbol).IsAssignableFrom(typeof(T)) && attr.AttributeClass is { IsGenericType: true, TypeArguments.Length: > 0 } attributeClass)
                    {
                        if (attributeMetadata.ArgumentPosition < attributeClass.TypeArguments.Length
                            && attributeClass.TypeArguments[attributeMetadata.ArgumentPosition] is { } typeSymbol)
                        {
                            return (T)typeSymbol;
                        }
                    }

                    var args = attr.ConstructorArguments;
                    if (attributeMetadata.ArgumentPosition >= args.Length)
                    {
                        if (attr.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax { ArgumentList: {} argumentList }
                            && attributeMetadata.ArgumentPosition < argumentList.Arguments.Count)
                        {
                            return semantic.GetConstantValue<T>(semanticModel, argumentList.Arguments[attributeMetadata.ArgumentPosition].Expression) ?? defaultValue;
                        }

                        throw new CompileErrorException($"The argument position {attributeMetadata.ArgumentPosition.ToString()} of attribute {attributeMetadata.Source} is out of range [0..{args.Length.ToString()}].", attributeMetadata.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                    }

                    var typedConstant = args[attributeMetadata.ArgumentPosition];
                    if (typedConstant.Value is T value)
                    {
                        return value;
                    }

                    break;

                case > 1:
                    throw new CompileErrorException($"{member} of the type {member.ContainingType} cannot be processed because it is marked with multiple mutually exclusive attributes.", attributeMetadata.Source.GetLocation(), LogId.ErrorInvalidMetadata);
            }
        }

        return defaultValue;
    }

    public T GetAttribute<TMdAttribute, T>(
        SemanticModel semanticModel,
        in ImmutableArray<TMdAttribute> metadata,
        in ImmutableArray<AttributeSyntax> attributes,
        T defaultValue)
        where TMdAttribute : IMdAttribute
    {
        if (metadata.IsDefaultOrEmpty || attributes.IsDefaultOrEmpty)
        {
            return defaultValue;
        }

        foreach (var attributeMetadata in metadata)
        {
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var attributeSyntax in attributes)
            {
                var args = attributeSyntax.ArgumentList?.Arguments.ToList() ?? [];
                if (attributeMetadata.ArgumentPosition >= args.Count)
                {
                    throw new CompileErrorException($"The argument position {attributeMetadata.ArgumentPosition.ToString()} of attribute {attributeMetadata.Source} is out of range [0..{args.Count.ToString()}].", attributeMetadata.Source.GetLocation(), LogId.ErrorInvalidMetadata);
                }

                var argSyntax = args[attributeMetadata.ArgumentPosition];
                var val = semantic.GetConstantValue<object>(semanticModel, argSyntax.Expression);
                if (val is T value)
                {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    private static IReadOnlyList<AttributeData> GetAttributes(ISymbol member, INamedTypeSymbol attributeType) =>
        member
            .GetAttributes()
            .Where(attr =>
                attr.AttributeClass != null
                && GetUnboundTypeSymbol(attr.AttributeClass)?.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat) == attributeType.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat))
            .ToArray();

    private static INamedTypeSymbol? GetUnboundTypeSymbol(INamedTypeSymbol? typeSymbol) =>
        typeSymbol is null
            ? typeSymbol
            : typeSymbol.IsGenericType
                ? typeSymbol.ConstructUnboundGenericType()
                : typeSymbol;
}