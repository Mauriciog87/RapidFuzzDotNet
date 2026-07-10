namespace RapidFuzz;

public readonly record struct ScoreAlignment(
    double Score,
    int SourceStart,
    int SourceEnd,
    int DestinationStart,
    int DestinationEnd);
