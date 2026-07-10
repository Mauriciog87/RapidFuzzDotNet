namespace RapidFuzz.Distance;

public readonly record struct LevenshteinWeights
{
    public LevenshteinWeights(int insertCost, int deleteCost, int replaceCost)
    {
        if (insertCost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(insertCost), "The insert cost must be greater than zero.");
        }

        if (deleteCost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deleteCost), "The delete cost must be greater than zero.");
        }

        if (replaceCost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(replaceCost), "The replace cost must be greater than zero.");
        }

        InsertCost = insertCost;
        DeleteCost = deleteCost;
        ReplaceCost = replaceCost;
    }

    public static LevenshteinWeights Default { get; } = new(1, 1, 1);

    public int InsertCost { get; }

    public int DeleteCost { get; }

    public int ReplaceCost { get; }
}
