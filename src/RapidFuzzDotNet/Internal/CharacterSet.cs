namespace RapidFuzz.Internal;

internal sealed class CharacterSet
{
    private const int AsciiLimit = 256;
    private readonly ulong[] ascii;
    private HashSet<char>? nonAscii;

    public CharacterSet()
    {
        ascii = new ulong[4];
    }

    public void Add(char value)
    {
        if (value < AsciiLimit)
        {
            int block = value >> 6;
            int bit = value & 63;
            ascii[block] |= 1UL << bit;
            return;
        }

        nonAscii ??= [];
        nonAscii.Add(value);
    }

    public bool Contains(char value)
    {
        if (value < AsciiLimit)
        {
            int block = value >> 6;
            int bit = value & 63;
            return (ascii[block] & (1UL << bit)) != 0;
        }

        return nonAscii is not null && nonAscii.Contains(value);
    }
}
