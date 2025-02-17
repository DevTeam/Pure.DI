// ReSharper disable HeapView.PossibleBoxingAllocation
// ReSharper disable UnusedMember.Global

namespace Build.Tools;

using Newtonsoft.Json;

[ExcludeFromCodeCoverage]
class Json
{
    private readonly JsonSerializer _serializer = JsonSerializer.Create();

    public T? TryDeserialize<T>(Stream inputJsonStream)
    {
        using var streamReader = new StreamReader(inputJsonStream);
        using var reader = new JsonTextReader(streamReader);
        return _serializer.Deserialize<T>(reader);
    }

    public void Serialize<T>(T value, Stream outputJsonStream)
    {
        using var streamWriter = new StreamWriter(outputJsonStream);
        using var writer = new JsonTextWriter(streamWriter);
        _serializer.Serialize(writer, value);
    }
}