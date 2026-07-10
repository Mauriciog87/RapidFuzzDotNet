namespace RapidFuzz.Internal;

internal interface DirectionMatrix
{
    void Set(int row, int column, byte value);

    byte Get(int row, int column);
}

internal sealed class PackedDirectionMatrix : DirectionMatrix
{
    public const byte Equal = 0;
    public const byte Insert = 1;
    public const byte Delete = 2;
    public const byte Replace = 3;

    private readonly byte[] data;
    private readonly int width;

    public PackedDirectionMatrix(int height, int width)
    {
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "The height must be positive.");
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The width must be positive.");
        }

        this.width = width;
        int cellCount = checked(height * width);
        data = new byte[(cellCount + 3) / 4];
    }

    public void Set(int row, int column, byte value)
    {
        int cell = checked((row * width) + column);
        int byteIndex = cell >> 2;
        int shift = (cell & 3) << 1;
        int clearMask = ~(3 << shift);
        data[byteIndex] = (byte)((data[byteIndex] & clearMask) | ((value & 3) << shift));
    }

    public byte Get(int row, int column)
    {
        int cell = checked((row * width) + column);
        int byteIndex = cell >> 2;
        int shift = (cell & 3) << 1;
        return (byte)((data[byteIndex] >> shift) & 3);
    }
}
