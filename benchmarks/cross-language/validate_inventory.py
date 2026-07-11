from __future__ import annotations

import argparse
import ast
import csv
import json
import re
import subprocess
from pathlib import Path


CPP_FILES = {
    "bench-lcs.cpp": 14,
    "bench-fuzz.cpp": 19,
    "bench-levenshtein.cpp": 18,
    "bench-jarowinkler.cpp": 26,
}
LENGTHS = {8, 16, 32, 63, 64, 65, 256, 1024}
PATTERNS = {"equal", "similar", "different", "repetitive"}
CORE_ALGORITHMS = {"levenshtein", "indel", "lcs_seq", "jaro", "jaro_winkler", "ratio"}
FUZZ_ALGORITHMS = {
    "ratio",
    "partial_ratio",
    "token_sort_ratio",
    "token_set_ratio",
    "partial_token_sort_ratio",
    "partial_token_set_ratio",
    "qratio",
    "wratio",
}


def git_head(directory: Path) -> str:
    return subprocess.check_output(
        ["git", "-C", str(directory), "rev-parse", "HEAD"],
        text=True,
        encoding="utf-8",
    ).strip()


def count_cpp_registrations(directory: Path) -> dict[str, int]:
    counts: dict[str, int] = {}
    for file_name, expected in CPP_FILES.items():
        content = (directory / "bench" / file_name).read_text(encoding="utf-8")
        count = len(re.findall(r"\bBENCHMARK\s*\(", content)) + len(
            re.findall(r"\bBENCHMARK_TEMPLATE\s*\(", content)
        )
        if count != expected:
            raise ValueError(f"{file_name} contains {count} registrations instead of {expected}.")
        counts[file_name] = count
    if sum(counts.values()) != 77:
        raise ValueError("The C++ benchmark inventory does not contain 77 registrations.")
    return counts


def python_scorers(path: Path) -> tuple[str, ...]:
    module = ast.parse(path.read_text(encoding="utf-8"), filename=str(path))
    for node in module.body:
        if isinstance(node, ast.Assign):
            for target in node.targets:
                if isinstance(target, ast.Name) and target.id == "LIBRARIES":
                    value = ast.literal_eval(node.value)
                    if isinstance(value, tuple) and len(value) == 8:
                        return tuple(str(item) for item in value)
    raise ValueError("The official Python benchmark does not expose eight LIBRARIES scorers.")


def dotnet_registrations(path: Path) -> tuple[str, ...]:
    content = path.read_text(encoding="utf-8")
    match = re.search(r"RegistrationNames\s*=\s*\[(.*?)\];", content, re.DOTALL)
    if match is None:
        raise ValueError("The .NET official registration inventory was not found.")
    values = tuple(re.findall(r'"([^"]+)"', match.group(1)))
    if len(values) != 77 or len(set(values)) != 77:
        raise ValueError("The .NET official registration inventory must contain 77 unique entries.")
    return values


def validate_corpus(directory: Path) -> dict[str, int]:
    with (directory / "cases.tsv").open(encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream, delimiter="\t"))
    identifiers = [row["case_id"] for row in rows]
    if len(identifiers) != len(set(identifiers)):
        raise ValueError("The common benchmark suite contains duplicate identifiers.")
    if len(rows) != 544:
        raise ValueError(f"The common benchmark suite contains {len(rows)} cases instead of 544.")
    static_dimensions: dict[tuple[str, str], set[tuple[int, str]]] = {}
    for row in rows:
        parts = row["case_id"].split("/")
        if row["category"] in {"core", "fuzz"} and row["mode"] == "static":
            static_dimensions.setdefault((row["category"], row["algorithm"]), set()).add(
                (int(parts[3]), parts[4])
            )
    expected_dimensions = {(length, pattern) for length in LENGTHS for pattern in PATTERNS}
    for algorithm in CORE_ALGORITHMS:
        if static_dimensions.get(("core", algorithm)) != expected_dimensions:
            raise ValueError(f"Core scorer '{algorithm}' does not cover all common dimensions.")
    for algorithm in FUZZ_ALGORITHMS:
        if static_dimensions.get(("fuzz", algorithm)) != expected_dimensions:
            raise ValueError(f"Fuzz scorer '{algorithm}' does not cover all common dimensions.")
    batch = {row["algorithm"] for row in rows if row["category"] == "batch"}
    if batch != FUZZ_ALGORITHMS:
        raise ValueError("The common batch suite does not contain all eight scorers.")
    return {
        "case_count": len(rows),
        "core_count": sum(row["category"] == "core" for row in rows),
        "fuzz_count": sum(row["category"] == "fuzz" for row in rows),
        "batch_count": sum(row["category"] == "batch" for row in rows),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cpp-source", type=Path, required=True)
    parser.add_argument("--python-source", type=Path, required=True)
    parser.add_argument("--corpus", type=Path, required=True)
    parser.add_argument("--dotnet-source", type=Path, required=True)
    parser.add_argument("--versions", type=Path, required=True)
    arguments = parser.parse_args()
    versions = json.loads(arguments.versions.resolve().read_text(encoding="utf-8"))
    cpp_source = arguments.cpp_source.resolve()
    python_source = arguments.python_source.resolve()
    cpp_head = git_head(cpp_source)
    python_head = git_head(python_source)
    if cpp_head != versions["rapidfuzz_cpp"]:
        raise ValueError("rapidfuzz-cpp checkout does not match the pinned SHA.")
    if python_head != versions["rapidfuzz_python"]:
        raise ValueError("RapidFuzz Python checkout does not match the pinned SHA.")
    document = {
        "rapidfuzz_cpp_sha": cpp_head,
        "rapidfuzz_python_sha": python_head,
        "cpp_registrations": count_cpp_registrations(cpp_source),
        "cpp_registration_count": 77,
        "python_scorers": python_scorers(python_source / "bench" / "benchmark_cdist.py"),
        "dotnet_registration_count": len(dotnet_registrations(arguments.dotnet_source.resolve())),
        "common": validate_corpus(arguments.corpus.resolve()),
    }
    print(json.dumps(document, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
