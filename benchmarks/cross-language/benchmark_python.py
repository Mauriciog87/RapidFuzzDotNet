from __future__ import annotations

import csv
import json
import os
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

import numpy as np
import pyperf
from rapidfuzz import fuzz, process
from rapidfuzz.distance import Indel, Jaro, JaroWinkler, LCSseq, Levenshtein


TIER_RANKS = {"smoke": 0, "common": 1, "all": 2}
FNV_OFFSET = 14695981039346656037
FNV_PRIME = 1099511628211
FNV_MASK = (1 << 64) - 1
QUANTIZATION_SCALE = 1_000_000_000_000.0


@dataclass(frozen=True)
class BenchmarkCase:
    case_id: str
    category: str
    algorithm: str
    mode: str
    source: str
    target: str
    score_cutoff: float


def corpus_directory() -> Path:
    configured = os.environ.get("RAPIDFUZZ_BENCHMARK_CORPUS")
    if configured:
        directory = Path(configured).resolve()
    else:
        directory = (Path.cwd() / "artifacts" / "benchmark-corpus").resolve()
    if not directory.is_dir():
        raise FileNotFoundError(f"Benchmark corpus was not found at '{directory}'.")
    return directory


def read_pairs(directory: Path) -> dict[str, tuple[str, str]]:
    pairs: dict[str, tuple[str, str]] = {}
    with (directory / "pairs.tsv").open(encoding="utf-8", newline="") as stream:
        for row in csv.DictReader(stream, delimiter="\t"):
            pair_id = row["pair_id"]
            if pair_id in pairs:
                raise ValueError(f"Duplicate benchmark pair '{pair_id}'.")
            pairs[pair_id] = (row["source"], row["target"])
    return pairs


def read_cases(directory: Path, runtime: str = "python") -> list[BenchmarkCase]:
    tier = os.environ.get("RAPIDFUZZ_BENCHMARK_TIER", "common")
    if tier not in TIER_RANKS:
        raise ValueError(f"Unknown benchmark tier '{tier}'.")
    selected_rank = TIER_RANKS[tier]
    pairs = read_pairs(directory)
    cases: list[BenchmarkCase] = []
    with (directory / "cases.tsv").open(encoding="utf-8", newline="") as stream:
        for row in csv.DictReader(stream, delimiter="\t"):
            runtimes = row["runtimes"].split(",")
            minimum_tier = row["minimum_tier"]
            if runtime not in runtimes or TIER_RANKS[minimum_tier] > selected_rank:
                continue
            source = ""
            target = ""
            pair_id = row["pair_id"]
            if pair_id:
                source, target = pairs[pair_id]
            cases.append(
                BenchmarkCase(
                    row["case_id"],
                    row["category"],
                    row["algorithm"],
                    row["mode"],
                    source,
                    target,
                    float(row["score_cutoff"]),
                )
            )
    return cases


def scorer_for(algorithm: str) -> Callable[..., float]:
    scorers: dict[str, Callable[..., float]] = {
        "ratio": fuzz.ratio,
        "partial_ratio": fuzz.partial_ratio,
        "token_sort_ratio": fuzz.token_sort_ratio,
        "token_set_ratio": fuzz.token_set_ratio,
        "partial_token_sort_ratio": fuzz.partial_token_sort_ratio,
        "partial_token_set_ratio": fuzz.partial_token_set_ratio,
        "qratio": fuzz.QRatio,
        "wratio": fuzz.WRatio,
    }
    try:
        return scorers[algorithm]
    except KeyError as error:
        raise ValueError(f"Unknown fuzz scorer '{algorithm}'.") from error


def enforce_similarity_cutoff(score: float, score_cutoff: float, uses_cutoff: bool) -> float:
    return score if not uses_cutoff or score >= score_cutoff else 0.0


def measure_individual(benchmark_case: BenchmarkCase) -> float:
    cutoff = benchmark_case.score_cutoff
    source = benchmark_case.source
    target = benchmark_case.target
    uses_cutoff = benchmark_case.mode == "cutoff"
    if benchmark_case.category == "fuzz":
        scorer = scorer_for(benchmark_case.algorithm)
        return float(scorer(source, target, score_cutoff=cutoff) if uses_cutoff else scorer(source, target))
    if benchmark_case.algorithm == "levenshtein":
        return float(Levenshtein.distance(source, target, score_cutoff=int(cutoff)) if uses_cutoff else Levenshtein.distance(source, target))
    if benchmark_case.algorithm == "indel":
        return float(Indel.distance(source, target, score_cutoff=int(cutoff)) if uses_cutoff else Indel.distance(source, target))
    if benchmark_case.algorithm == "lcs_seq":
        return float(LCSseq.similarity(source, target, score_cutoff=int(cutoff)) if uses_cutoff else LCSseq.similarity(source, target))
    if benchmark_case.algorithm == "jaro":
        score = float(Jaro.similarity(source, target, score_cutoff=cutoff) if uses_cutoff else Jaro.similarity(source, target))
        return enforce_similarity_cutoff(score, cutoff, uses_cutoff)
    if benchmark_case.algorithm == "jaro_winkler":
        score = float(JaroWinkler.similarity(source, target, prefix_weight=0.1, score_cutoff=cutoff) if uses_cutoff else JaroWinkler.similarity(source, target))
        return enforce_similarity_cutoff(score, cutoff, uses_cutoff)
    if benchmark_case.algorithm == "ratio":
        return float(fuzz.ratio(source, target, score_cutoff=cutoff) if uses_cutoff else fuzz.ratio(source, target))
    raise ValueError(f"Unknown core scorer '{benchmark_case.algorithm}'.")


def individual_function(benchmark_case: BenchmarkCase) -> Callable[[], float]:
    def invoke() -> float:
        return measure_individual(benchmark_case)

    return invoke


def batch_function(benchmark_case: BenchmarkCase, queries: list[str], choices: list[str]) -> Callable[[], np.ndarray]:
    scorer = scorer_for(benchmark_case.algorithm)

    def invoke() -> np.ndarray:
        return process.cdist(queries, choices, scorer=scorer, dtype=np.float64, workers=1)

    return invoke


def digest_matrix(matrix: np.ndarray) -> tuple[str, list[float]]:
    flattened = np.asarray(matrix, dtype=np.float64).reshape(-1)
    quantized = np.floor(flattened * QUANTIZATION_SCALE + 0.5).astype("<i8", copy=False)
    hash_value = FNV_OFFSET
    for byte_value in memoryview(quantized.tobytes()):
        hash_value ^= byte_value
        hash_value = (hash_value * FNV_PRIME) & FNV_MASK
    middle = len(flattened) // 2
    samples = [float(flattened[0]), float(flattened[middle]), float(flattened[-1])]
    return f"{hash_value:016x}", samples


def validate(output_path: Path) -> int:
    directory = corpus_directory()
    cases = read_cases(directory)
    results: list[dict[str, object]] = []
    queries = (directory / "queries.txt").read_text(encoding="utf-8").splitlines()
    choices = (directory / "choices.txt").read_text(encoding="utf-8").splitlines()
    for benchmark_case in cases:
        if benchmark_case.category == "batch":
            matrix = batch_function(benchmark_case, queries, choices)()
            digest, samples = digest_matrix(matrix)
            results.append(
                {
                    "case_id": benchmark_case.case_id,
                    "value": None,
                    "digest": digest,
                    "count": int(matrix.size),
                    "samples": samples,
                }
            )
        else:
            value = measure_individual(benchmark_case)
            results.append(
                {
                    "case_id": benchmark_case.case_id,
                    "value": value,
                    "digest": None,
                    "count": 1,
                    "samples": [value],
                }
            )
    metadata = json.loads((directory / "metadata.json").read_text(encoding="utf-8"))
    document = {
        "runtime": os.environ.get("RAPIDFUZZ_BENCHMARK_RUNTIME", "python"),
        "corpus_sha256": metadata["corpus_sha256"],
        "results": results,
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(document, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
    return 0


def run_benchmarks() -> int:
    directory = corpus_directory()
    cases = read_cases(directory)
    profile = os.environ.get("RAPIDFUZZ_BENCHMARK_PROFILE", "common")
    processes = 1 if profile == "smoke" else 5
    values = 1 if profile == "smoke" else 3
    warmups = 1 if profile == "smoke" else 2
    minimum_time = 0.01 if profile == "smoke" else 0.1
    runner = pyperf.Runner(processes=processes, values=values, warmups=warmups, min_time=minimum_time)
    runner.metadata["corpus_sha256"] = json.loads((directory / "metadata.json").read_text(encoding="utf-8"))["corpus_sha256"]
    runner.metadata["workers"] = 1
    queries = (directory / "queries.txt").read_text(encoding="utf-8").splitlines()
    choices = (directory / "choices.txt").read_text(encoding="utf-8").splitlines()
    for benchmark_case in cases:
        if benchmark_case.category == "batch":
            runner.bench_func(benchmark_case.case_id, batch_function(benchmark_case, queries, choices))
        else:
            runner.bench_func(benchmark_case.case_id, individual_function(benchmark_case))
    return 0


def main() -> int:
    if "--validate-only" in sys.argv:
        arguments = list(sys.argv[1:])
        arguments.remove("--validate-only")
        if len(arguments) != 2 or arguments[0] != "--output":
            raise ValueError("Validation requires --validate-only --output <path>.")
        return validate(Path(arguments[1]).resolve())
    return run_benchmarks()


if __name__ == "__main__":
    raise SystemExit(main())
