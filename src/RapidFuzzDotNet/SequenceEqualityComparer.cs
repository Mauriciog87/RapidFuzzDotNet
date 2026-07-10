namespace RapidFuzz;

public interface ISequenceEqualityComparer<in TLeft, in TRight>
{
    bool Equals(TLeft left, TRight right);
}
