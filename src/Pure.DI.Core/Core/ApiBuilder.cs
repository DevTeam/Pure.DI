// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable HeapView.DelegateAllocation

namespace Pure.DI.Core;

sealed class ApiBuilder(IResources resources)
    : IBuilder<Unit, IEnumerable<Source>>
{
    private static readonly Encoding Utf8EncodingWithNoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    public IEnumerable<Source> Build(Unit data)
    {
        foreach (var resource in resources.GetResource("""^[\w\.]+\.g\.cs$"""))
        {
            string text;
            Encoding actualEncoding;
            using (resource)
            {
                text = Decode(resource.Content, Utf8EncodingWithNoBom, out actualEncoding);
            }

            text = text.Replace("/*A2768DE22DE3E430C9653990D516CC9B_LockField*/", $"\"{Names.LockFieldName}\"");
            yield return new Source(resource.Name, SourceText.From(text, actualEncoding));
        }
    }

    private static string Decode(Stream stream, Encoding encoding, out Encoding actualEncoding)
    {
        const int maxBufferSize = 4096;
        var bufferSize = maxBufferSize;

        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var length = (int)stream.Length;
            if (length == 0)
            {
                actualEncoding = encoding;
                return string.Empty;
            }

            bufferSize = Math.Min(maxBufferSize, length);
        }

        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: bufferSize, leaveOpen: true);
        var text = reader.ReadToEnd();
        actualEncoding = reader.CurrentEncoding;
        return text;
    }
}