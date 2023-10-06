namespace Pure.DI.Core.Code;

internal static class TypeSymbolExtensions
{
    public static bool IsAtomicValueType(this ITypeSymbol typeSymbol) =>
        typeSymbol.SpecialType switch
        {
            SpecialType.System_Enum => typeSymbol.BaseType is { } baseType && baseType.IsAtomicValueType(),
            SpecialType.System_SByte => true,
            SpecialType.System_Byte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Char => true,
            SpecialType.System_Boolean => true,
            SpecialType.System_Single => true,
            _ => false
        };
}