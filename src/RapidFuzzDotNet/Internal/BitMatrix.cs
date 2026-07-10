namespace RapidFuzz.Internal;

internal sealed class BitMatrix
{
    private readonly ulong[] data;
    private readonly int width;

    public BitMatrix(int height, int width)
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
        data = new ulong[(cellCount + 63) / 64];
    }

    public void Set(int row, int column, bool value)
    {
        int cell = checked((row * width) + column);
        int wordIndex = cell >> 6;
        ulong mask = 1UL << (cell & 63);

        if (value)
        {
            data[wordIndex] |= mask;
        }
        else
        {
            data[wordIndex] &= ~mask;
        }
    }

    public bool Get(int row, int column)
    {
        int cell = checked((row * width) + column);
        int wordIndex = cell >> 6;
        ulong mask = 1UL << (cell & 63);
        return (data[wordIndex] & mask) != 0UL;
    }
}

internal sealed class ShiftedBitMatrix
{
    private readonly BitMatrix matrix;
    private readonly int rowOffset;
    private readonly int columnOffset;

    public ShiftedBitMatrix(int height, int width, int rowOffset, int columnOffset)
    {
        matrix = new BitMatrix(height, width);
        this.rowOffset = rowOffset;
        this.columnOffset = columnOffset;
    }

    public void Set(int row, int column, bool value)
    {
        matrix.Set(row - rowOffset, column - columnOffset, value);
    }

    public bool Get(int row, int column)
    {
        return matrix.Get(row - rowOffset, column - columnOffset);
    }
}

internal sealed class BitTraceMatrix : DirectionMatrix
{
    private readonly ShiftedBitMatrix lowBits;
    private readonly ShiftedBitMatrix highBits;

    public BitTraceMatrix(int height, int width)
    {
        lowBits = new ShiftedBitMatrix(height, width, 0, 0);
        highBits = new ShiftedBitMatrix(height, width, 0, 0);
    }

    public void Set(int row, int column, byte value)
    {
        lowBits.Set(row, column, (value & 1) != 0);
        highBits.Set(row, column, (value & 2) != 0);
    }

    public byte Get(int row, int column)
    {
        byte value = 0;

        if (lowBits.Get(row, column))
        {
            value |= 1;
        }

        if (highBits.Get(row, column))
        {
            value |= 2;
        }

        return value;
    }
}

internal static class TraceMatrixFactory
{
    private const int PackedCellLimit = 4096;

    public static DirectionMatrix Create(int height, int width)
    {
        int cellCount = checked(height * width);
        return cellCount <= PackedCellLimit
            ? new PackedDirectionMatrix(height, width)
            : new BitTraceMatrix(height, width);
    }
}
