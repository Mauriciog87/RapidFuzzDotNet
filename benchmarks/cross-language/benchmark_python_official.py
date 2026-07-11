from __future__ import annotations

import argparse
import json
import os
import statistics
import time
from pathlib import Path
from typing import Callable

import numpy as np
from rapidfuzz import fuzz, process, utils


SCORERS = (
    "ratio",
    "partial_ratio",
    "token_sort_ratio",
    "token_set_ratio",
    "partial_token_sort_ratio",
    "partial_token_set_ratio",
    "QRatio",
    "WRatio",
)
PROCESSED_SCORERS = {
    "token_sort_ratio",
    "token_set_ratio",
    "partial_token_sort_ratio",
    "partial_token_set_ratio",
    "QRatio",
    "WRatio",
}


def corpus_directory() -> Path:
    configured = os.environ.get("RAPIDFUZZ_BENCHMARK_CORPUS")
    directory = Path(configured).resolve() if configured else (Path.cwd() / "artifacts" / "benchmark-corpus").resolve()
    if not directory.is_dir():
        raise FileNotFoundError(f"Benchmark corpus was not found at '{directory}'.")
    return directory


def rapidfuzz_scorer(name: str) -> Callable[..., float]:
    return getattr(fuzz, name)


def fuzzywuzzy_scorer(name: str) -> Callable[[str, str], int]:
    from fuzzywuzzy import fuzz as fuzzywuzzy_fuzz

    return getattr(fuzzywuzzy_fuzz, name)


def measure(function: Callable[[], object], repeats: int) -> list[float]:
    durations: list[float] = []
    function()
    for _ in range(repeats):
        started = time.perf_counter_ns()
        function()
        durations.append((time.perf_counter_ns() - started) / 1_000_000_000.0)
    return durations


def summarize(runtime: str, scorer: str, durations: list[float], cell_count: int) -> dict[str, object]:
    median_seconds = statistics.median(durations)
    deviation = statistics.pstdev(durations) if len(durations) > 1 else 0.0
    coefficient = deviation / statistics.fmean(durations) if statistics.fmean(durations) > 0 else 0.0
    return {
        "runtime": runtime,
        "scorer": scorer,
        "durations_seconds": durations,
        "median_seconds": median_seconds,
        "ns_per_cell": median_seconds * 1_000_000_000.0 / cell_count,
        "cells_per_second": cell_count / median_seconds,
        "coefficient_of_variation": coefficient,
        "stable": coefficient <= 0.1,
    }


def run(include_fuzzywuzzy: bool, repeats: int) -> dict[str, object]:
    directory = corpus_directory()
    choices = (directory / "choices.txt").read_text(encoding="utf-8").splitlines()
    queries = (directory / "queries.txt").read_text(encoding="utf-8").splitlines()
    metadata = json.loads((directory / "metadata.json").read_text(encoding="utf-8"))
    cell_count = len(queries) * len(choices)
    results: list[dict[str, object]] = []
    for scorer_name in SCORERS:
        scorer = rapidfuzz_scorer(scorer_name)
        processor = utils.default_process if scorer_name in PROCESSED_SCORERS else None

        def rapidfuzz_call(active_scorer: Callable[..., float] = scorer, active_processor: Callable[[str], str] | None = processor) -> np.ndarray:
            return process.cdist(
                queries,
                choices,
                scorer=active_scorer,
                processor=active_processor,
                dtype=np.float64,
                workers=1,
            )

        results.append(summarize("rapidfuzz-python", scorer_name, measure(rapidfuzz_call, repeats), cell_count))
        if include_fuzzywuzzy:
            legacy_scorer = fuzzywuzzy_scorer(scorer_name)
            if scorer_name in PROCESSED_SCORERS:
                from fuzzywuzzy import utils as fuzzywuzzy_utils

                prepared_queries = [fuzzywuzzy_utils.full_process(value) for value in queries]
                prepared_choices = [fuzzywuzzy_utils.full_process(value) for value in choices]
            else:
                prepared_queries = queries
                prepared_choices = choices

            def fuzzywuzzy_call(active_scorer: Callable[[str, str], int] = legacy_scorer) -> None:
                for query in prepared_queries:
                    for choice in prepared_choices:
                        active_scorer(query, choice)

            results.append(summarize("fuzzywuzzy", scorer_name, measure(fuzzywuzzy_call, 1), cell_count))
    return {
        "corpus_sha256": metadata["corpus_sha256"],
        "query_count": len(queries),
        "choice_count": len(choices),
        "cell_count": cell_count,
        "workers": 1,
        "results": results,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--include-fuzzywuzzy", action="store_true")
    parser.add_argument("--repeats", type=int, default=5)
    arguments = parser.parse_args()
    if arguments.repeats < 1:
        raise ValueError("Repeats must be positive.")
    document = run(arguments.include_fuzzywuzzy, arguments.repeats)
    output_path = arguments.output.resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(document, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
