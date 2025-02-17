// ReSharper disable StringLiteralTypo

namespace Pure.DI.Core.Code;

using System.Globalization;

static class CodeExtensions
{
    public static string ValueToString(this object? tag, string defaultValue = "null") =>
        tag switch
        {
            string => $"\"{tag}\"",
            double val => $"{val.ToString(CultureInfo.InvariantCulture)}D",
            float val => $"{val.ToString(CultureInfo.InvariantCulture)}F",
            decimal val => $"{val.ToString(CultureInfo.InvariantCulture)}M",
            uint => $"{tag}U",
            long => $"{tag}L",
            ulong => $"{tag}UL",
            sbyte => $"(sbyte){tag}",
            byte => $"(byte){tag}",
            short => $"(short){tag}",
            ushort => $"(ushort){tag}",
            nint => $"(nint){tag}",
            nuint => $"(nuint){tag}",
            char val => $"'{val}'",
            Enum val => $"{val.GetType()}.{val}",
            ITypeSymbol val => $"typeof({val.ToDisplayString()})",
            MdTagOnSites => defaultValue,
            not null => tag.ToString(),
            _ => defaultValue
        };
}