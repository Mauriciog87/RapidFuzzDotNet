namespace RapidFuzz;

public delegate double Scorer(string query, string choice, double scoreCutoff);

public sealed record ExtractedResult<TChoice>(TChoice Choice, double Score, int Index);

public static class Process
{
    public static List<ExtractedResult<string>> Extract(
        string query,
        IEnumerable<string> choices,
        Scorer? scorer = null,
        Func<string, string>? processor = null,
        int? limit = 5,
        double scoreCutoff = 0.0)
    {
        return Extract(
            query,
            choices,
            static choice => choice,
            scorer,
            processor,
            limit,
            scoreCutoff);
    }

    public static List<ExtractedResult<TChoice>> Extract<TChoice>(
        string query,
        IEnumerable<TChoice> choices,
        Func<TChoice, string> choiceSelector,
        Scorer? scorer = null,
        Func<string, string>? processor = null,
        int? limit = 5,
        double scoreCutoff = 0.0)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(choiceSelector);

        if (limit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "The limit must be greater than or equal to zero.");
        }

        if (limit == 0)
        {
            return [];
        }

        List<ExtractedResult<TChoice>> results = ExtractIter(
            query,
            choices,
            choiceSelector,
            scorer,
            processor,
            scoreCutoff).ToList();

        results.Sort(static (left, right) =>
        {
            int scoreComparison = right.Score.CompareTo(left.Score);
            return scoreComparison != 0 ? scoreComparison : left.Index.CompareTo(right.Index);
        });

        if (limit is null || results.Count <= limit.Value)
        {
            return results;
        }

        return results.GetRange(0, limit.Value);
    }

    public static IEnumerable<ExtractedResult<string>> ExtractIter(
        string query,
        IEnumerable<string> choices,
        Scorer? scorer = null,
        Func<string, string>? processor = null,
        double scoreCutoff = 0.0)
    {
        return ExtractIter(query, choices, static choice => choice, scorer, processor, scoreCutoff);
    }

    public static IEnumerable<ExtractedResult<TChoice>> ExtractIter<TChoice>(
        string query,
        IEnumerable<TChoice> choices,
        Func<TChoice, string> choiceSelector,
        Scorer? scorer = null,
        Func<string, string>? processor = null,
        double scoreCutoff = 0.0)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(choiceSelector);

        return ExtractIterCore(query, choices, choiceSelector, scorer, processor, scoreCutoff);
    }

    public static ExtractedResult<string>? ExtractOne(
        string query,
        IEnumerable<string> choices,
        Scorer? scorer = null,
        Func<string, string>? processor = null,
        double scoreCutoff = 0.0)
    {
        List<ExtractedResult<string>> results = Extract(query, choices, scorer, processor, 1, scoreCutoff);
        return results.Count == 0 ? null : results[0];
    }

    public static ExtractedResult<TChoice>? ExtractOne<TChoice>(
        string query,
        IEnumerable<TChoice> choices,
        Func<TChoice, string> choiceSelector,
        Scorer? scorer = null,
        Func<string, string>? processor = null,
        double scoreCutoff = 0.0)
    {
        List<ExtractedResult<TChoice>> results = Extract(query, choices, choiceSelector, scorer, processor, 1, scoreCutoff);
        return results.Count == 0 ? null : results[0];
    }

    public static double[,] Cdist(
        IEnumerable<string> queries,
        IEnumerable<string> choices,
        Scorer? scorer = null,
        Func<string, string>? processor = null,
        double scoreCutoff = 0.0)
    {
        return Cdist(
            queries,
            choices,
            static query => query,
            static choice => choice,
            scorer,
            processor,
            scoreCutoff);
    }

    public static double[,] Cdist<TQuery, TChoice>(
        IEnumerable<TQuery> queries,
        IEnumerable<TChoice> choices,
        Func<TQuery, string> querySelector,
        Func<TChoice, string> choiceSelector,
        Scorer? scorer = null,
        Func<string, string>? processor = null,
        double scoreCutoff = 0.0)
    {
        List<string> preparedQueries = MaterializePrepared(queries, querySelector, processor, nameof(queries), nameof(querySelector));
        List<string> preparedChoices = MaterializePrepared(choices, choiceSelector, processor, nameof(choices), nameof(choiceSelector));
        double[,] results = new double[preparedQueries.Count, preparedChoices.Count];

        for (int queryIndex = 0; queryIndex < preparedQueries.Count; queryIndex++)
        {
            string preparedQuery = preparedQueries[queryIndex];
            Scorer activeScorer = scorer ?? DefaultScorer;
            Func<string, double, double>? cachedScorer = CreateCachedScorer(scorer, preparedQuery);

            for (int choiceIndex = 0; choiceIndex < preparedChoices.Count; choiceIndex++)
            {
                results[queryIndex, choiceIndex] = cachedScorer is null
                    ? activeScorer(preparedQuery, preparedChoices[choiceIndex], scoreCutoff)
                    : cachedScorer(preparedChoices[choiceIndex], scoreCutoff);
            }
        }

        return results;
    }

    public static double[] Cpdist(
        IEnumerable<string> queries,
        IEnumerable<string> choices,
        Scorer? scorer = null,
        Func<string, string>? processor = null,
        double scoreCutoff = 0.0)
    {
        return Cpdist(
            queries,
            choices,
            static query => query,
            static choice => choice,
            scorer,
            processor,
            scoreCutoff);
    }

    public static double[] Cpdist<TQuery, TChoice>(
        IEnumerable<TQuery> queries,
        IEnumerable<TChoice> choices,
        Func<TQuery, string> querySelector,
        Func<TChoice, string> choiceSelector,
        Scorer? scorer = null,
        Func<string, string>? processor = null,
        double scoreCutoff = 0.0)
    {
        List<string> preparedQueries = MaterializePrepared(queries, querySelector, processor, nameof(queries), nameof(querySelector));
        List<string> preparedChoices = MaterializePrepared(choices, choiceSelector, processor, nameof(choices), nameof(choiceSelector));

        if (preparedQueries.Count != preparedChoices.Count)
        {
            throw new ArgumentException("Queries and choices must have the same number of elements.");
        }

        double[] results = new double[preparedQueries.Count];

        for (int index = 0; index < preparedQueries.Count; index++)
        {
            string preparedQuery = preparedQueries[index];
            Scorer activeScorer = scorer ?? DefaultScorer;
            Func<string, double, double>? cachedScorer = CreateCachedScorer(scorer, preparedQuery);
            results[index] = cachedScorer is null
                ? activeScorer(preparedQuery, preparedChoices[index], scoreCutoff)
                : cachedScorer(preparedChoices[index], scoreCutoff);
        }

        return results;
    }

    private static IEnumerable<ExtractedResult<TChoice>> ExtractIterCore<TChoice>(
        string query,
        IEnumerable<TChoice> choices,
        Func<TChoice, string> choiceSelector,
        Scorer? scorer,
        Func<string, string>? processor,
        double scoreCutoff)
    {
        string preparedQuery = processor is null ? query : processor(query);
        Scorer activeScorer = scorer ?? DefaultScorer;
        Func<string, double, double>? cachedScorer = CreateCachedScorer(scorer, preparedQuery);
        int index = 0;

        foreach (TChoice choice in choices)
        {
            if (choice is null)
            {
                throw new ArgumentException("Choices cannot contain null values.", nameof(choices));
            }

            string selectedChoice = choiceSelector(choice);

            if (selectedChoice is null)
            {
                throw new ArgumentException("The choice selector cannot return null.", nameof(choiceSelector));
            }

            string preparedChoice = processor is null ? selectedChoice : processor(selectedChoice);
            double score = cachedScorer is null
                ? activeScorer(preparedQuery, preparedChoice, scoreCutoff)
                : cachedScorer(preparedChoice, scoreCutoff);

            if (score >= scoreCutoff)
            {
                yield return new ExtractedResult<TChoice>(choice, score, index);
            }

            index++;
        }
    }

    private static List<string> MaterializePrepared<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, string> selector,
        Func<string, string>? processor,
        string itemsName,
        string selectorName)
    {
        ArgumentNullException.ThrowIfNull(items, itemsName);
        ArgumentNullException.ThrowIfNull(selector, selectorName);

        List<string> prepared = [];

        foreach (TItem item in items)
        {
            if (item is null)
            {
                throw new ArgumentException("Sequences cannot contain null values.", itemsName);
            }

            string selected = selector(item);

            if (selected is null)
            {
                throw new ArgumentException("Selectors cannot return null.", selectorName);
            }

            prepared.Add(processor is null ? selected : processor(selected));
        }

        return prepared;
    }

    private static double DefaultScorer(string query, string choice, double scoreCutoff)
    {
        return Fuzz.WRatio(query, choice, scoreCutoff);
    }

    private static Func<string, double, double>? CreateCachedScorer(Scorer? scorer, string preparedQuery)
    {
        if (scorer is null)
        {
            CachedWRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        Scorer ratio = Fuzz.Ratio;
        if (scorer == ratio)
        {
            CachedRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        Scorer partialRatio = Fuzz.PartialRatio;
        if (scorer == partialRatio)
        {
            CachedPartialRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        Scorer tokenSortRatio = Fuzz.TokenSortRatio;
        if (scorer == tokenSortRatio)
        {
            CachedTokenSortRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        Scorer partialTokenSortRatio = Fuzz.PartialTokenSortRatio;
        if (scorer == partialTokenSortRatio)
        {
            CachedPartialTokenSortRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        Scorer tokenSetRatio = Fuzz.TokenSetRatio;
        if (scorer == tokenSetRatio)
        {
            CachedTokenSetRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        Scorer partialTokenSetRatio = Fuzz.PartialTokenSetRatio;
        if (scorer == partialTokenSetRatio)
        {
            CachedPartialTokenSetRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        Scorer tokenRatio = Fuzz.TokenRatio;
        if (scorer == tokenRatio)
        {
            CachedTokenRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        Scorer partialTokenRatio = Fuzz.PartialTokenRatio;
        if (scorer == partialTokenRatio)
        {
            CachedPartialTokenRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        Scorer qRatio = Fuzz.QRatio;
        if (scorer == qRatio)
        {
            CachedQRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        Scorer wRatio = Fuzz.WRatio;
        if (scorer == wRatio)
        {
            CachedWRatio cachedScorer = new(preparedQuery);
            return (choice, scoreCutoff) => cachedScorer.Similarity(choice, scoreCutoff);
        }

        return null;
    }
}
