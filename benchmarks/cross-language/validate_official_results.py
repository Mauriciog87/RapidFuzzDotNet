from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


CPP_EXECUTABLES = {
    "bench_lcs": 25,
    "bench_levenshtein": 28,
    "bench_jarowinkler": 36,
    "bench_fuzz": 19,
}
DOTNET_CLASSES = {
    "OfficialLcsLongBenchmarks": 13,
    "OfficialLcsStaticBenchmarks": 4,
    "OfficialLcsCachedBenchmarks": 4,
    "OfficialLcsSimdBenchmarks": 4,
    "OfficialLevenshteinLongBenchmarks": 12,
    "OfficialLevenshteinWeightedBenchmarks": 4,
    "OfficialLevenshteinStaticBenchmarks": 4,
    "OfficialLevenshteinCachedBenchmarks": 4,
    "OfficialLevenshteinSimdBenchmarks": 4,
    "OfficialJaroLongBenchmarks": 12,
    "OfficialJaroStaticBenchmarks": 8,
    "OfficialJaroCachedBenchmarks": 8,
    "OfficialJaroSimdBenchmarks": 8,
    "OfficialFuzzBenchmarks": 19,
}
PYTHON_SCORERS = {
    "ratio",
    "partial_ratio",
    "token_sort_ratio",
    "token_set_ratio",
    "partial_token_sort_ratio",
    "partial_token_set_ratio",
    "QRatio",
    "WRatio",
}


def google_count(path: Path) -> int:
    document = json.loads(path.read_text(encoding="utf-8"))
    names = {
        entry.get("run_name") or entry.get("name")
        for entry in document.get("benchmarks", [])
        if entry.get("run_type") != "aggregate"
    }
    if None in names:
        raise ValueError(f"Google Benchmark result '{path}' contains an unnamed workload.")
    return len(names)


def dotnet_counts(directory: Path) -> dict[str, int]:
    paths = sorted(directory.rglob("*-report-full.json"))
    if not paths:
        raise ValueError(f"No BenchmarkDotNet full JSON reports were found in '{directory}'.")
    names: set[str] = set()
    counts: dict[str, int] = {}
    for path in paths:
        document = json.loads(path.read_text(encoding="utf-8"))
        for benchmark in document.get("Benchmarks", []):
            full_name = benchmark.get("FullName")
            if not isinstance(full_name, str):
                raise ValueError(f"BenchmarkDotNet result '{path}' contains an unnamed workload.")
            if full_name in names:
                raise ValueError(f"Duplicate BenchmarkDotNet workload '{full_name}'.")
            names.add(full_name)
            match = re.search(r"\.([A-Za-z0-9]+Benchmarks)\.", full_name)
            if match is None:
                raise ValueError(f"BenchmarkDotNet workload '{full_name}' has no class identifier.")
            class_name = match.group(1)
            counts[class_name] = counts.get(class_name, 0) + 1
    return counts


def validate(root: Path, expected_corpus_sha: str) -> dict[str, object]:
    cpp: dict[str, dict[str, int]] = {}
    for compiler in ("gcc", "clang"):
        compiler_counts: dict[str, int] = {}
        for executable, expected in CPP_EXECUTABLES.items():
            path = root / f"cpp-{compiler}" / f"{executable}.json"
            actual = google_count(path)
            if actual != expected:
                raise ValueError(f"{compiler} {executable} contains {actual} workloads instead of {expected}.")
            compiler_counts[executable] = actual
        cpp[compiler] = compiler_counts
    dotnet: dict[str, dict[str, int]] = {}
    for runtime in ("dotnet-net8", "dotnet-net10"):
        actual = dotnet_counts(root / runtime)
        if actual != DOTNET_CLASSES:
            raise ValueError(f"{runtime} official workload inventory does not match the expected expansion.")
        dotnet[runtime] = actual
    python_document = json.loads((root / "python-corrected.json").read_text(encoding="utf-8"))
    if python_document.get("corpus_sha256") != expected_corpus_sha:
        raise ValueError("The official Python benchmark corpus SHA does not match the generated corpus.")
    if (
        python_document.get("query_count") != 100
        or python_document.get("choice_count") != 10000
        or python_document.get("cell_count") != 1000000
        or python_document.get("workers") != 1
    ):
        raise ValueError("The official Python benchmark shape is not 100 by 10,000 on one worker.")
    python_scorers = {result.get("scorer") for result in python_document.get("results", [])}
    if python_scorers != PYTHON_SCORERS:
        raise ValueError("The corrected Python benchmark does not contain all eight official scorers.")
    original_log = root / "python-original.log"
    original_exit_path = root / "python-original.exit-code"
    if not original_log.is_file() or original_log.stat().st_size == 0 or not original_exit_path.is_file():
        raise ValueError("The original Python benchmark log or exit code is missing.")
    original_exit_code = int(original_exit_path.read_text(encoding="utf-8").strip())
    return {
        "cpp": cpp,
        "cpp_registration_count": 77,
        "expanded_workload_count": sum(CPP_EXECUTABLES.values()),
        "dotnet": dotnet,
        "python_scorer_count": len(python_scorers),
        "python_original_exit_code": original_exit_code,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--official-root", type=Path, required=True)
    parser.add_argument("--corpus", type=Path, required=True)
    arguments = parser.parse_args()
    corpus_metadata = json.loads((arguments.corpus.resolve() / "metadata.json").read_text(encoding="utf-8"))
    print(
        json.dumps(
            validate(arguments.official_root.resolve(), str(corpus_metadata["corpus_sha256"])),
            sort_keys=True,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
