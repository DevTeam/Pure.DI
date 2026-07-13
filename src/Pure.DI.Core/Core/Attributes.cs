// ReSharper disable HeapView.DelegateAllocation

// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

sealed class Attributes(
    ISemantic semantic,
    ISymbolNames symbolNames,
    ILocationProvider locationProvider)
    : IAttributes
{
    public T GetAttribute<TMdAttribute, T>(
        SemanticModel semanticModel,
        in ImmutableArray<TMdAttribute> metadata,
        ISymbol member,
        AttributeKind kind,
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
                            && attributeClass.TypeArguments[attributeMetadata.ArgumentPosition] is {} typeSymbol)
                        {
                            return (T)typeSymbol;
                        }
                    }

                    var args = attr.ConstructorArguments;
                    if (attributeMetadata.ArgumentPosition >= args.Length || args[attributeMetadata.ArgumentPosition].Kind == TypedConstantKind.Error)
                    {
                        if (attr.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax { ArgumentList: {} argumentList }
                            && attributeMetadata.ArgumentPosition < argumentList.Arguments.Count)
                        {
                            return semantic.GetConstantValue<T>(semanticModel, argumentList.Arguments[attributeMetadata.ArgumentPosition].Expression, GetSmartTagKind(kind))
                                   ?? defaultValue;
                        }
                    }

                    if (attributeMetadata.ArgumentPosition >= args.Length)
                    {
                        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                        return kind switch
                        {
                            AttributeKind.Ordinal when typeof(T).IsAssignableFrom(typeof(int)) => (T)(object)0,
                            _ => throw new CompileErrorException(
                                string.Format(Strings.Error_Template_InvalidAttributeArgumentPosition, attributeMetadata.ArgumentPosition, attributeMetadata.Source, args.Length),
                                ImmutableArray.Create(locationProvider.GetLocation(attributeMetadata.Source)),
                                LogId.ErrorInvalidAttributeArgumentPosition,
                                nameof(Strings.Error_Template_InvalidAttributeArgumentPosition))
                        };
                    }

                    var typedConstant = args[attributeMetadata.ArgumentPosition];
                    if (typedConstant.Value is T value)
                    {
                        return value;
                    }

                    break;

                case > 1:
                    throw new CompileErrorException(
                        string.Format(Strings.Error_Template_AttributeMemberCannotBeProcessed, member, member.ContainingType),
                        ImmutableArray.Create(locationProvider.GetLocation(attributeMetadata.Source)),
                        LogId.ErrorAttributeMemberCannotBeProcessed,
                        nameof(Strings.Error_Template_AttributeMemberCannotBeProcessed));
            }
        }

        return defaultValue;
    }

    public T GetAttribute<TMdAttribute, T>(
        SemanticModel semanticModel,
        in ImmutableArray<TMdAttribute> metadata,
        in ImmutableArray<AttributeSyntax> attributes,
        AttributeKind kind,
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
                    // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                    return kind switch
                    {
                        AttributeKind.Ordinal when typeof(T).IsAssignableFrom(typeof(int)) => (T)(object)0,
                        _ => throw new CompileErrorException(
                            string.Format(Strings.Error_Template_InvalidAttributeArgumentPosition, attributeMetadata.ArgumentPosition, attributeMetadata.Source, args.Count),
                            ImmutableArray.Create(locationProvider.GetLocation(attributeMetadata.Source)),
                            LogId.ErrorInvalidAttributeArgumentPosition,
                            nameof(Strings.Error_Template_InvalidAttributeArgumentPosition))
                    };
                }

                var argSyntax = args[attributeMetadata.ArgumentPosition];
                var val = semantic.GetConstantValue<object>(semanticModel, argSyntax.Expression, GetSmartTagKind(kind));
                if (val is T value)
                {
                    return value;
                }
            }
        }

        return defaultValue;
    }

    private static SmartTagKind GetSmartTagKind(AttributeKind kind) =>
        kind == AttributeKind.Tag ? SmartTagKind.Tag : SmartTagKind.Unknown;

    private IReadOnlyList<AttributeData> GetAttributes(ISymbol member, INamedTypeSymbol attributeType) =>
        member
            .GetAttributes()
            .Where(attr => {
                if (attr.AttributeClass is null)
                {
                    return false;
                }

                var unboundTypeSymbol = GetUnboundTypeSymbol(attr.AttributeClass);
                if (unboundTypeSymbol is null)
                {
                    return false;
                }

                return symbolNames.GetGlobalName(unboundTypeSymbol) == symbolNames.GetGlobalName(attributeType);
            })
            .ToList();

    private static INamedTypeSymbol? GetUnboundTypeSymbol(INamedTypeSymbol? typeSymbol) =>
        typeSymbol is null
            ? typeSymbol
            : typeSymbol.IsGenericType
                ? typeSymbol.ConstructUnboundGenericType()
                : typeSymbol;
}
