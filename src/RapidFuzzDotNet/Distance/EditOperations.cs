using System.Collections;
using System.Text;

namespace RapidFuzz.Distance;

public enum EditOperation
{
    Equal,
    Replace,
    Insert,
    Delete
}

public readonly record struct EditOp(EditOperation Operation, int SourcePosition, int DestinationPosition);

public readonly record struct Opcode(
    EditOperation Operation,
    int SourceStart,
    int SourceEnd,
    int DestinationStart,
    int DestinationEnd);

public readonly record struct MatchingBlock(int SourcePosition, int DestinationPosition, int Length);

public sealed class EditOperations : IReadOnlyList<EditOp>, IEquatable<EditOperations>
{
    private readonly List<EditOp> operations;

    public EditOperations(IEnumerable<EditOp> operations, int sourceLength, int destinationLength)
    {
        ArgumentNullException.ThrowIfNull(operations);

        if (sourceLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceLength), "The source length must be non-negative.");
        }

        if (destinationLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationLength), "The destination length must be non-negative.");
        }

        this.operations = operations.ToList();
        SourceLength = sourceLength;
        DestinationLength = destinationLength;

        ValidateOperations();
    }

    public int SourceLength { get; }

    public int DestinationLength { get; }

    public int Count => operations.Count;

    public EditOp this[int index] => operations[index];

    public bool Equals(EditOperations? other)
    {
        if (other is null)
        {
            return false;
        }

        return SourceLength == other.SourceLength
            && DestinationLength == other.DestinationLength
            && operations.SequenceEqual(other.operations);
    }

    public override bool Equals(object? obj)
    {
        return obj is EditOperations other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(SourceLength);
        hashCode.Add(DestinationLength);

        foreach (EditOp operation in operations)
        {
            hashCode.Add(operation);
        }

        return hashCode.ToHashCode();
    }

    public IEnumerator<EditOp> GetEnumerator()
    {
        return operations.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Opcodes ToOpcodes()
    {
        List<Opcode> result = [];
        int sourcePosition = 0;
        int destinationPosition = 0;

        foreach (EditOp operation in operations)
        {
            if (sourcePosition < operation.SourcePosition || destinationPosition < operation.DestinationPosition)
            {
                result.Add(new Opcode(
                    EditOperation.Equal,
                    sourcePosition,
                    operation.SourcePosition,
                    destinationPosition,
                    operation.DestinationPosition));
            }

            switch (operation.Operation)
            {
                case EditOperation.Replace:
                    result.Add(new Opcode(
                        EditOperation.Replace,
                        operation.SourcePosition,
                        operation.SourcePosition + 1,
                        operation.DestinationPosition,
                        operation.DestinationPosition + 1));
                    sourcePosition = operation.SourcePosition + 1;
                    destinationPosition = operation.DestinationPosition + 1;
                    break;
                case EditOperation.Insert:
                    result.Add(new Opcode(
                        EditOperation.Insert,
                        operation.SourcePosition,
                        operation.SourcePosition,
                        operation.DestinationPosition,
                        operation.DestinationPosition + 1));
                    sourcePosition = operation.SourcePosition;
                    destinationPosition = operation.DestinationPosition + 1;
                    break;
                case EditOperation.Delete:
                    result.Add(new Opcode(
                        EditOperation.Delete,
                        operation.SourcePosition,
                        operation.SourcePosition + 1,
                        operation.DestinationPosition,
                        operation.DestinationPosition));
                    sourcePosition = operation.SourcePosition + 1;
                    destinationPosition = operation.DestinationPosition;
                    break;
                case EditOperation.Equal:
                    break;
                default:
                    throw new InvalidOperationException("Unknown edit operation.");
            }
        }

        if (sourcePosition < SourceLength || destinationPosition < DestinationLength)
        {
            result.Add(new Opcode(
                EditOperation.Equal,
                sourcePosition,
                SourceLength,
                destinationPosition,
                DestinationLength));
        }

        return new Opcodes(result, SourceLength, DestinationLength);
    }

    public EditOperations Inverse()
    {
        List<EditOp> result = new(operations.Count);

        foreach (EditOp operation in operations)
        {
            EditOperation inverseOperation = operation.Operation switch
            {
                EditOperation.Insert => EditOperation.Delete,
                EditOperation.Delete => EditOperation.Insert,
                _ => operation.Operation
            };

            result.Add(new EditOp(inverseOperation, operation.DestinationPosition, operation.SourcePosition));
        }

        return new EditOperations(result, DestinationLength, SourceLength);
    }

    public EditOperations Reverse()
    {
        List<EditOp> result = new(operations);
        result.Reverse();
        return new EditOperations(result, SourceLength, DestinationLength);
    }

    public EditOperations Slice(int start, int stop, int step = 1)
    {
        SliceBounds bounds = SliceBounds.Create(Count, start, stop, step);
        List<EditOp> result = new(bounds.SelectionCount);

        for (int index = bounds.Start; index < bounds.Stop; index += step)
        {
            result.Add(operations[index]);
        }

        return new EditOperations(result, SourceLength, DestinationLength);
    }

    public EditOperations RemoveSlice(int start, int stop, int step = 1)
    {
        SliceBounds bounds = SliceBounds.Create(Count, start, stop, step);
        List<EditOp> result = new(Count - bounds.SelectionCount);

        for (int index = 0; index < Count; index++)
        {
            bool selected = index >= bounds.Start
                && index < bounds.Stop
                && (index - bounds.Start) % step == 0;

            if (!selected)
            {
                result.Add(operations[index]);
            }
        }

        return new EditOperations(result, SourceLength, DestinationLength);
    }

    public EditOperations RemoveSubsequence(EditOperations subsequence)
    {
        ArgumentNullException.ThrowIfNull(subsequence);

        if (subsequence.Count > Count)
        {
            throw new ArgumentException("The edit operations do not contain the subsequence.", nameof(subsequence));
        }

        List<EditOp> result = new(Count - subsequence.Count);
        int offset = 0;
        int operationIndex = 0;

        foreach (EditOp subsequenceOperation in subsequence)
        {
            bool found = false;

            while (operationIndex < operations.Count)
            {
                EditOp operation = operations[operationIndex];

                if (operation == subsequenceOperation)
                {
                    found = true;
                    break;
                }

                result.Add(OffsetSourcePosition(operation, offset));
                operationIndex++;
            }

            if (!found)
            {
                throw new ArgumentException("The edit operations do not contain the subsequence.", nameof(subsequence));
            }

            offset = subsequenceOperation.Operation switch
            {
                EditOperation.Insert => offset + 1,
                EditOperation.Delete => offset - 1,
                _ => offset
            };
            operationIndex++;
        }

        while (operationIndex < operations.Count)
        {
            result.Add(OffsetSourcePosition(operations[operationIndex], offset));
            operationIndex++;
        }

        return new EditOperations(result, SourceLength, DestinationLength);
    }

    public MatchingBlock[] GetMatchingBlocks()
    {
        return ToOpcodes().GetMatchingBlocks();
    }

    public string ApplyTo(string source, string destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ValidateApplyLengths(source.Length, destination.Length);

        StringBuilder builder = new(destination.Length);
        int sourcePosition = 0;

        foreach (EditOp operation in operations)
        {
            if (operation.SourcePosition > sourcePosition)
            {
                builder.Append(source, sourcePosition, operation.SourcePosition - sourcePosition);
                sourcePosition = operation.SourcePosition;
            }

            switch (operation.Operation)
            {
                case EditOperation.Replace:
                    builder.Append(destination[operation.DestinationPosition]);
                    sourcePosition++;
                    break;
                case EditOperation.Insert:
                    builder.Append(destination[operation.DestinationPosition]);
                    break;
                case EditOperation.Delete:
                    sourcePosition++;
                    break;
                case EditOperation.Equal:
                    break;
                default:
                    throw new InvalidOperationException("Unknown edit operation.");
            }
        }

        if (sourcePosition < source.Length)
        {
            builder.Append(source, sourcePosition, source.Length - sourcePosition);
        }

        return builder.ToString();
    }

    public char[] ApplyTo(ReadOnlySpan<char> source, ReadOnlySpan<char> destination)
    {
        return ApplyTo<char>(source, destination);
    }

    public T[] ApplyTo<T>(ReadOnlySpan<T> source, ReadOnlySpan<T> destination)
    {
        ValidateApplyLengths(source.Length, destination.Length);

        List<T> result = new(destination.Length);
        int sourcePosition = 0;

        foreach (EditOp operation in operations)
        {
            if (operation.SourcePosition > sourcePosition)
            {
                for (int i = sourcePosition; i < operation.SourcePosition; i++)
                {
                    result.Add(source[i]);
                }

                sourcePosition = operation.SourcePosition;
            }

            switch (operation.Operation)
            {
                case EditOperation.Replace:
                    result.Add(destination[operation.DestinationPosition]);
                    sourcePosition++;
                    break;
                case EditOperation.Insert:
                    result.Add(destination[operation.DestinationPosition]);
                    break;
                case EditOperation.Delete:
                    sourcePosition++;
                    break;
                case EditOperation.Equal:
                    break;
                default:
                    throw new InvalidOperationException("Unknown edit operation.");
            }
        }

        for (int i = sourcePosition; i < source.Length; i++)
        {
            result.Add(source[i]);
        }

        return result.ToArray();
    }

    private void ValidateApplyLengths(int sourceLength, int destinationLength)
    {
        if (sourceLength != SourceLength)
        {
            throw new ArgumentException("The source length does not match the edit operations.", nameof(sourceLength));
        }

        if (destinationLength != DestinationLength)
        {
            throw new ArgumentException("The destination length does not match the edit operations.", nameof(destinationLength));
        }
    }

    private static EditOp OffsetSourcePosition(EditOp operation, int offset)
    {
        return operation with { SourcePosition = operation.SourcePosition + offset };
    }

    private void ValidateOperations()
    {
        foreach (EditOp operation in operations)
        {
            ValidateOperation(operation);
        }
    }

    private void ValidateOperation(EditOp operation)
    {
        switch (operation.Operation)
        {
            case EditOperation.Replace:
                ValidateSourceIndex(operation.SourcePosition);
                ValidateDestinationIndex(operation.DestinationPosition);
                break;
            case EditOperation.Insert:
                ValidateSourceBoundary(operation.SourcePosition);
                ValidateDestinationIndex(operation.DestinationPosition);
                break;
            case EditOperation.Delete:
                ValidateSourceIndex(operation.SourcePosition);
                ValidateDestinationBoundary(operation.DestinationPosition);
                break;
            case EditOperation.Equal:
                ValidateSourceBoundary(operation.SourcePosition);
                ValidateDestinationBoundary(operation.DestinationPosition);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), "The edit operation is invalid.");
        }
    }

    private void ValidateSourceIndex(int position)
    {
        if (position < 0 || position >= SourceLength)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The source position is outside the source.");
        }
    }

    private void ValidateDestinationIndex(int position)
    {
        if (position < 0 || position >= DestinationLength)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The destination position is outside the destination.");
        }
    }

    private void ValidateSourceBoundary(int position)
    {
        if (position < 0 || position > SourceLength)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The source position is outside the source.");
        }
    }

    private void ValidateDestinationBoundary(int position)
    {
        if (position < 0 || position > DestinationLength)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The destination position is outside the destination.");
        }
    }
}

public sealed class Opcodes : IReadOnlyList<Opcode>, IEquatable<Opcodes>
{
    private readonly List<Opcode> opcodes;

    public Opcodes(IEnumerable<Opcode> opcodes, int sourceLength, int destinationLength)
    {
        ArgumentNullException.ThrowIfNull(opcodes);

        if (sourceLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceLength), "The source length must be non-negative.");
        }

        if (destinationLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationLength), "The destination length must be non-negative.");
        }

        this.opcodes = opcodes.ToList();
        SourceLength = sourceLength;
        DestinationLength = destinationLength;

        ValidateOpcodes();
    }

    public int SourceLength { get; }

    public int DestinationLength { get; }

    public int Count => opcodes.Count;

    public Opcode this[int index] => opcodes[index];

    public bool Equals(Opcodes? other)
    {
        if (other is null)
        {
            return false;
        }

        return SourceLength == other.SourceLength
            && DestinationLength == other.DestinationLength
            && opcodes.SequenceEqual(other.opcodes);
    }

    public override bool Equals(object? obj)
    {
        return obj is Opcodes other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(SourceLength);
        hashCode.Add(DestinationLength);

        foreach (Opcode opcode in opcodes)
        {
            hashCode.Add(opcode);
        }

        return hashCode.ToHashCode();
    }

    public IEnumerator<Opcode> GetEnumerator()
    {
        return opcodes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public EditOperations ToEditOperations()
    {
        List<EditOp> result = [];

        foreach (Opcode opcode in opcodes)
        {
            switch (opcode.Operation)
            {
                case EditOperation.Equal:
                    break;
                case EditOperation.Replace:
                    AddReplaceOperations(result, opcode);
                    break;
                case EditOperation.Insert:
                    AddInsertOperations(result, opcode);
                    break;
                case EditOperation.Delete:
                    AddDeleteOperations(result, opcode);
                    break;
                default:
                    throw new InvalidOperationException("Unknown edit operation.");
            }
        }

        return new EditOperations(result, SourceLength, DestinationLength);
    }

    public Opcodes Inverse()
    {
        List<Opcode> result = new(opcodes.Count);

        foreach (Opcode opcode in opcodes)
        {
            EditOperation inverseOperation = opcode.Operation switch
            {
                EditOperation.Insert => EditOperation.Delete,
                EditOperation.Delete => EditOperation.Insert,
                _ => opcode.Operation
            };

            result.Add(new Opcode(
                inverseOperation,
                opcode.DestinationStart,
                opcode.DestinationEnd,
                opcode.SourceStart,
                opcode.SourceEnd));
        }

        return new Opcodes(result, DestinationLength, SourceLength);
    }

    public Opcodes Reverse()
    {
        List<Opcode> result = new(opcodes);
        result.Reverse();
        return new Opcodes(result, SourceLength, DestinationLength);
    }

    public Opcodes Slice(int start, int stop, int step = 1)
    {
        SliceBounds bounds = SliceBounds.Create(Count, start, stop, step);
        List<Opcode> result = new(bounds.SelectionCount);

        for (int index = bounds.Start; index < bounds.Stop; index += step)
        {
            result.Add(opcodes[index]);
        }

        return new Opcodes(result, SourceLength, DestinationLength);
    }

    public MatchingBlock[] GetMatchingBlocks()
    {
        List<MatchingBlock> blocks = [];

        foreach (Opcode opcode in opcodes)
        {
            if (opcode.Operation != EditOperation.Equal)
            {
                continue;
            }

            int length = opcode.SourceEnd - opcode.SourceStart;

            if (length > 0)
            {
                blocks.Add(new MatchingBlock(opcode.SourceStart, opcode.DestinationStart, length));
            }
        }

        blocks.Add(new MatchingBlock(SourceLength, DestinationLength, 0));
        return blocks.ToArray();
    }

    public string ApplyTo(string source, string destination)
    {
        return ToEditOperations().ApplyTo(source, destination);
    }

    public char[] ApplyTo(ReadOnlySpan<char> source, ReadOnlySpan<char> destination)
    {
        return ToEditOperations().ApplyTo(source, destination);
    }

    public T[] ApplyTo<T>(ReadOnlySpan<T> source, ReadOnlySpan<T> destination)
    {
        return ToEditOperations().ApplyTo(source, destination);
    }

    private static void AddReplaceOperations(List<EditOp> result, Opcode opcode)
    {
        int sourceLength = opcode.SourceEnd - opcode.SourceStart;
        int destinationLength = opcode.DestinationEnd - opcode.DestinationStart;
        int replaceLength = Math.Min(sourceLength, destinationLength);

        for (int offset = 0; offset < replaceLength; offset++)
        {
            result.Add(new EditOp(
                EditOperation.Replace,
                opcode.SourceStart + offset,
                opcode.DestinationStart + offset));
        }

        for (int offset = replaceLength; offset < sourceLength; offset++)
        {
            result.Add(new EditOp(
                EditOperation.Delete,
                opcode.SourceStart + offset,
                opcode.DestinationStart + replaceLength));
        }

        for (int offset = replaceLength; offset < destinationLength; offset++)
        {
            result.Add(new EditOp(
                EditOperation.Insert,
                opcode.SourceStart + replaceLength,
                opcode.DestinationStart + offset));
        }
    }

    private static void AddInsertOperations(List<EditOp> result, Opcode opcode)
    {
        for (int destinationPosition = opcode.DestinationStart; destinationPosition < opcode.DestinationEnd; destinationPosition++)
        {
            result.Add(new EditOp(EditOperation.Insert, opcode.SourceStart, destinationPosition));
        }
    }

    private static void AddDeleteOperations(List<EditOp> result, Opcode opcode)
    {
        for (int sourcePosition = opcode.SourceStart; sourcePosition < opcode.SourceEnd; sourcePosition++)
        {
            result.Add(new EditOp(EditOperation.Delete, sourcePosition, opcode.DestinationStart));
        }
    }

    private void ValidateOpcodes()
    {
        foreach (Opcode opcode in opcodes)
        {
            ValidateOpcode(opcode);
        }
    }

    private void ValidateOpcode(Opcode opcode)
    {
        ValidateRange(opcode.SourceStart, opcode.SourceEnd, SourceLength);
        ValidateRange(opcode.DestinationStart, opcode.DestinationEnd, DestinationLength);

        switch (opcode.Operation)
        {
            case EditOperation.Equal:
            case EditOperation.Replace:
                break;
            case EditOperation.Insert:
                if (opcode.SourceStart != opcode.SourceEnd)
                {
                    throw new ArgumentOutOfRangeException(nameof(opcode), "Insert opcodes must have an empty source range.");
                }

                break;
            case EditOperation.Delete:
                if (opcode.DestinationStart != opcode.DestinationEnd)
                {
                    throw new ArgumentOutOfRangeException(nameof(opcode), "Delete opcodes must have an empty destination range.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(opcode), "The opcode operation is invalid.");
        }
    }

    private static void ValidateRange(int start, int end, int length)
    {
        if (start < 0 || end < start || end > length)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "The opcode range is invalid.");
        }
    }
}

internal readonly record struct SliceBounds(int Start, int Stop, int SelectionCount)
{
    public static SliceBounds Create(int count, int start, int stop, int step)
    {
        if (step <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step), "The slice step must be positive.");
        }

        int normalizedStart = NormalizeIndex(count, start);
        int normalizedStop = NormalizeIndex(count, stop);
        int selectionCount = normalizedStart >= normalizedStop
            ? 0
            : ((normalizedStop - normalizedStart - 1) / step) + 1;
        return new SliceBounds(normalizedStart, normalizedStop, selectionCount);
    }

    private static int NormalizeIndex(int count, int index)
    {
        long normalized = index < 0 ? (long)count + index : index;
        return (int)Math.Clamp(normalized, 0L, count);
    }
}
