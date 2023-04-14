// ReSharper disable StringLiteralTypo
namespace Pure.DI.Core.CSharp;

internal static class CodeExtensions
{
    public static string ValueToString(this object? tag, string defaultValue = "null") => 
        tag switch
        {
            string => $"\"{tag}\"",
            double => $"{tag}D",
            float => $"{tag}F",
            decimal => $"{tag}M",
            uint => $"{tag}U",
            long => $"{tag}L",
            ulong => $"{tag}UL",
            sbyte => $"(sbyte){tag}",
            byte => $"(byte){tag}",
            short => $"(short){tag}",
            ushort => $"(ushort){tag}",
            nint => $"(nint){tag}",
            nuint => $"(nuint){tag}",
            char ch => $"'{ch.ToString()}'",
            Enum en => $"{en.GetType()}.{en}",
            ITypeSymbol => $"typeof({tag})",
            not null => tag.ToString(),
            _ => defaultValue
        };
}