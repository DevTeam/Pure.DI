namespace Pure.DI.Core.Incremental;

// FNV-1a, 64 bit: fast, stable between runs and processes, and with collisions too rare to matter here.
struct Hasher()
{
    private const ulong Prime = 1099511628211UL;

    public ulong Value { get; private set; } = 14695981039346656037UL;

    public void Add(ulong value)
    {
        for (var i = 0; i < 8; i++)
        {
            var lowByte = value & 0xFFUL;
            Value = (Value ^ lowByte) * Prime;
            value >>= 8;
        }
    }

    // The unsigned conversion is intentional: a negative value must hash as the 32-bit pattern it has in memory.
    public void Add(int value)
    {
        var unsigned = (uint)value;
        Add(unsigned);
    }

    public void Add(bool value) => Add(value ? 1UL : 0UL);

    public void Add(string? value)
    {
        if (value is null)
        {
            Add(ulong.MaxValue);
            return;
        }

        foreach (var ch in value)
        {
            Value = (Value ^ ch) * Prime;
        }

        // Separates strings, so "ab" + "c" and "a" + "bc" give different hashes.
        Add(value.Length);
    }

    private void Add(SourceText text)
    {
        var buffer = new char[4096];
        for (var position = 0; position < text.Length; position += buffer.Length)
        {
            var count = Math.Min(buffer.Length, text.Length - position);
            text.CopyTo(position, buffer, 0, count);
            for (var i = 0; i < count; i++)
            {
                Value = (Value ^ buffer[i]) * Prime;
            }
        }

        Add(text.Length);
    }

    public static ulong Hash(string value)
    {
        var hasher = new Hasher();
        hasher.Add(value);
        return hasher.Value;
    }

    public static ulong Hash(SourceText text)
    {
        var hasher = new Hasher();
        hasher.Add(text);
        return hasher.Value;
    }
}
