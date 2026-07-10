# RapidFuzzDotNet

RapidFuzzDotNet is a pure managed C# implementation of the public algorithms in `rapidfuzz-cpp`. It targets `net8.0` and `net10.0`, has no runtime package dependencies, and ships portable symbols with Source Link.

Version `1.0.0-beta.2` is the first publication candidate with complete tracked parity against upstream commit `b5830af53bd1b3c7460a8de1e9f7095df99b3470`.

## Install

```powershell
dotnet add package RapidFuzzDotNet --version 1.0.0-beta.2
```

## Fuzz And Distance

```csharp
using RapidFuzz;
using RapidFuzz.Distance;

double ratio = Fuzz.Ratio("this is a test", "this is a test!");
double partial = Fuzz.PartialRatio("new york mets", "new york yankees");
int distance = Levenshtein.Distance("kitten", "sitting");
double jaro = JaroWinkler.Similarity("martha", "marhta");
```

Distance cutoffs return `scoreCutoff + 1` when the true result exceeds the cutoff. Similarity cutoffs return zero. A score hint may select a faster path but does not change the result.

## Generic Sequences

Generic APIs accept `ReadOnlySpan<T>` where `T` implements equality.

```csharp
using RapidFuzz;
using RapidFuzz.Distance;

int[] first = [1, 2, 3, 4];
int[] second = [1, 3, 2, 4];

int distance = DamerauLevenshtein.Distance<int>(first, second);
double score = Fuzz.QRatio<int>(first, second);
EditOperations edits = Levenshtein.Editops<int>(first, second);
int[] transformed = edits.ApplyTo<int>(first, second);
```

## Cached Scorers

Cached scorers defensively copy or materialize their source and reuse precomputed pattern state.

```csharp
using RapidFuzz;
using RapidFuzz.Distance;

CachedRatio cachedRatio = new("fuzzy wuzzy was a bear");
double ratio = cachedRatio.Similarity("wuzzy fuzzy was a bear");

CachedLevenshtein<int> cachedDistance = new([1, 2, 3, 4]);
int distance = cachedDistance.Distance([1, 3, 2, 4]);
```

## Multi Scorers

Multi scorers process one target against a batch of sources. Patterns up to 64 elements use portable `Vector<ulong>` lanes when hardware acceleration is available; longer patterns use cached scalar fallbacks.

```csharp
using RapidFuzz.Distance.Experimental;
using RapidFuzz.Experimental;

MultiLevenshtein multiDistance = new(["kitten", "sitting", "smitten"]);
int[] distances = multiDistance.Distances("sitting");

MultiQRatio<int> multiRatio = new([[1, 2, 3], [1, 3, 2]]);
double[] scores = multiRatio.Similarities([1, 2, 4]);
```

The `Experimental` multi APIs are mutable during construction and do not guarantee thread safety.

## Process

`Process.Extract`, `ExtractIter`, `ExtractOne`, `Cdist`, and `Cpdist` provide ranked search and score matrices over strings or selected values.

```csharp
using RapidFuzz;

string[] choices = ["new york mets", "new york yankees", "atlanta braves"];
ExtractedResult<string>? match = Process.ExtractOne("new york mets", choices);
double[,] scores = Process.Cdist(["new york mets"], choices, scorer: Fuzz.Ratio);
```

## Provenance

This project is an independent managed port derived from the MIT-licensed `rapidfuzz-cpp` project by Max Bachmann and contributors. The pinned upstream source and attribution are recorded in `NOTICE` and `LICENSE` inside the package.
