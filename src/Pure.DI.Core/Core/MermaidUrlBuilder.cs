// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using System.IO.Compression;

sealed class MermaidUrlBuilder : IBuilder<string, Uri>
{
    public Uri Build(string diagram)
    {
        var encoded = JsonEncode(diagram.Replace("\\", ""));
        var json = $"{{\"code\":\"{encoded}\",\"mermaid\":\"{{\\\"theme\\\":\\\"dark\\\"}}\"}}";
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var compressedBytes = Compress(jsonBytes);
        var pako = Convert.ToBase64String(compressedBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
        return new Uri($"https://mermaid.live/view#pako:{pako}", UriKind.Absolute);
    }

    private static string JsonEncode(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var sb = new StringBuilder(text.Length + 4);
        foreach (var ch in text)
        {
            switch (ch)
            {
                case ' ':
                    sb.Append(" ");
                    break;

                case '\\':
                case '"':
                case '/':
                    sb.Append('\\');
                    sb.Append(ch);
                    break;

                case '\b':
                    sb.Append("\\b");
                    break;

                case '\t':
                    sb.Append("\\t");
                    break;

                case '\n':
                    sb.Append("\\n");
                    break;

                case '\f':
                    sb.Append("\\f");
                    break;

                case '\r':
                    sb.Append("\\r");
                    break;

                default:
                    if (char.IsLetterOrDigit(ch))
                    {
                        sb.Append(ch);
                    }
                    else
                    {
                        sb.Append($"\\u{(uint)ch:X4}");
                    }

                    break;
            }
        }

        return sb.ToString();
    }

    private static byte[] Compress(byte[] data)
    {
        // Compute Adler-32
        uint adler1 = 1, adler2 = 0;
        foreach (var bt in data)
        {
            adler1 = (adler1 + bt) % 65521;
            adler2 = (adler2 + adler1) % 65521;
        }

        using var resultStream = new MemoryStream();

        // Write header
        resultStream.WriteByte(0x78);
        resultStream.WriteByte(0xDA);

        // Write compressed data
        using (var stream = new DeflateStream(resultStream, CompressionLevel.Optimal))
        {
            stream.Write(data, 0, data.Length);
        }

        data = resultStream.ToArray();

        // append the checksum-trailer:
        var adlerPos = data.Length;
        Array.Resize(ref data, adlerPos + 4);
        data[adlerPos] = (byte)(adler2 >> 8);
        data[adlerPos + 1] = (byte)adler2;
        data[adlerPos + 2] = (byte)(adler1 >> 8);
        data[adlerPos + 3] = (byte)adler1;
        return data;
    }
}