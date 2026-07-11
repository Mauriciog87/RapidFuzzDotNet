from __future__ import annotations

import argparse
import hashlib
import json
import random
import string
from collections import Counter
from pathlib import Path


LENGTHS = (8, 16, 32, 63, 64, 65, 256, 1024)
PATTERNS = ("equal", "similar", "different", "repetitive")
CORE_ALGORITHMS = ("levenshtein", "indel", "lcs_seq", "jaro", "jaro_winkler", "ratio")
FUZZ_ALGORITHMS = (
    "ratio",
    "partial_ratio",
    "token_sort_ratio",
    "token_set_ratio",
    "partial_token_sort_ratio",
    "partial_token_set_ratio",
    "qratio",
    "wratio",
)
TOKEN_ALGORITHMS = {
    "token_sort_ratio",
    "token_set_ratio",
    "partial_token_sort_ratio",
    "partial_token_set_ratio",
    "wratio",
}


def random_text(random_source: random.Random, length: int) -> str:
    alphabet = string.ascii_letters + string.digits
    return "".join(random_source.choice(alphabet) for _ in range(length))


def mutate_text(random_source: random.Random, value: str) -> str:
    alphabet = string.ascii_letters + string.digits
    characters = list(value)
    mutation_count = max(1, len(characters) // 16)
    for index in random_source.sample(range(len(characters)), mutation_count):
        replacement = random_source.choice(alphabet)
        if replacement == characters[index]:
            replacement = alphabet[(alphabet.index(replacement) + 1) % len(alphabet)]
        characters[index] = replacement
    return "".join(characters)


def create_pair(random_source: random.Random, length: int, pattern: str) -> tuple[str, str]:
    if pattern == "different":
        return "a" * length, "b" * length
    if pattern == "repetitive":
        if length == 1:
            return "a", "b"
        return "a" + ("b" * (length - 2)) + "a", "b" * length
    source = random_text(random_source, length)
    if pattern == "equal":
        return source, source
    return source, mutate_text(random_source, source)


def create_token_pair(random_source: random.Random, length: int, pattern: str) -> tuple[str, str]:
    token_count = max(2, length // 6)
    source_tokens = [random_text(random_source, 5) for _ in range(token_count)]
    if pattern == "equal":
        target_tokens = list(source_tokens)
    elif pattern == "similar":
        target_tokens = source_tokens[1:] + source_tokens[:1]
        target_tokens[-1] = mutate_text(random_source, target_tokens[-1])
    elif pattern == "different":
        source_tokens = ["aaaaa" for _ in range(token_count)]
        target_tokens = ["bbbbb" for _ in range(token_count)]
    else:
        source_tokens = ["alpha" if index % 2 == 0 else "beta" for index in range(token_count)]
        target_tokens = ["beta" if index % 3 == 0 else "alpha" for index in range(token_count)]
    return " ".join(source_tokens), " ".join(target_tokens)


def core_tier(algorithm: str, length: int, pattern: str, mode: str) -> str:
    if mode == "static" and algorithm in {"levenshtein", "jaro", "ratio"} and length in {8, 64} and pattern == "similar":
        return "smoke"
    if mode == "cutoff" and algorithm in {"jaro", "jaro_winkler"} and length == 64 and pattern == "similar":
        return "smoke"
    if mode == "cutoff":
        return "common"
    if mode == "cached" and length == 64 and pattern == "similar" and algorithm in {"levenshtein", "jaro", "ratio"}:
        return "smoke"
    return "common"


def fuzz_tier(algorithm: str, length: int, pattern: str, mode: str) -> str:
    if mode == "static" and algorithm in {"ratio", "partial_ratio"} and length == 64 and pattern == "similar":
        return "smoke"
    return "common"


def create_cases() -> list[tuple[str, str, str, str, str, str, str, str]]:
    cases: list[tuple[str, str, str, str, str, str, str, str]] = []
    for algorithm in CORE_ALGORITHMS:
        for length in LENGTHS:
            for pattern in PATTERNS:
                pair_id = f"pair-{length}-{pattern}"
                case_id = f"core/{algorithm}/static/{length}/{pattern}"
                cases.append((case_id, "core", algorithm, "static", pair_id, "0", core_tier(algorithm, length, pattern, "static"), "cpp,dotnet,python"))
        for length in (64, 256):
            for pattern in ("similar", "different"):
                pair_id = f"pair-{length}-{pattern}"
                cutoff = "0.9" if algorithm in {"jaro", "jaro_winkler"} else "90" if algorithm == "ratio" else "30"
                case_id = f"core/{algorithm}/cutoff/{length}/{pattern}"
                cases.append((case_id, "core", algorithm, "cutoff", pair_id, cutoff, core_tier(algorithm, length, pattern, "cutoff"), "cpp,dotnet,python"))
        for length in (8, 64, 256, 1024):
            for pattern in ("similar", "different"):
                pair_id = f"pair-{length}-{pattern}"
                case_id = f"core/{algorithm}/cached/{length}/{pattern}"
                cases.append((case_id, "core", algorithm, "cached", pair_id, "0", core_tier(algorithm, length, pattern, "cached"), "cpp,dotnet"))
    for algorithm in FUZZ_ALGORITHMS:
        for length in LENGTHS:
            for pattern in PATTERNS:
                pair_prefix = "token" if algorithm in TOKEN_ALGORITHMS else "pair"
                pair_id = f"{pair_prefix}-{length}-{pattern}"
                case_id = f"fuzz/{algorithm}/static/{length}/{pattern}"
                cases.append((case_id, "fuzz", algorithm, "static", pair_id, "0", fuzz_tier(algorithm, length, pattern, "static"), "cpp,dotnet,python"))
        for pattern in ("similar", "different"):
            pair_prefix = "token" if algorithm in TOKEN_ALGORITHMS else "pair"
            pair_id = f"{pair_prefix}-64-{pattern}"
            case_id = f"fuzz/{algorithm}/cutoff/64/{pattern}"
            cases.append((case_id, "fuzz", algorithm, "cutoff", pair_id, "90", "common", "cpp,dotnet,python"))
    for algorithm in FUZZ_ALGORITHMS:
        tier = "smoke" if algorithm == "ratio" else "common"
        case_id = f"batch/{algorithm}"
        cases.append((case_id, "batch", algorithm, "static", "", "0", tier, "cpp,dotnet,python"))
    return cases


def write_tsv(path: Path, header: tuple[str, ...], rows: list[tuple[str, ...]]) -> None:
    content = "\n".join(("\t".join(header), *("\t".join(row) for row in rows))) + "\n"
    path.write_text(content, encoding="utf-8", newline="\n")


def file_hash(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def generate(output: Path, seed: int) -> dict[str, object]:
    output.mkdir(parents=True, exist_ok=True)
    pair_random = random.Random(seed)
    pair_rows: list[tuple[str, ...]] = []
    for length in LENGTHS:
        for pattern in PATTERNS:
            source, target = create_pair(pair_random, length, pattern)
            pair_rows.append((f"pair-{length}-{pattern}", str(length), pattern, source, target))
    for length in LENGTHS:
        for pattern in PATTERNS:
            source, target = create_token_pair(pair_random, length, pattern)
            pair_rows.append((f"token-{length}-{pattern}", str(length), pattern, source, target))
    pair_path = output / "pairs.tsv"
    write_tsv(pair_path, ("pair_id", "length", "pattern", "source", "target"), pair_rows)

    cases = create_cases()
    case_path = output / "cases.tsv"
    write_tsv(
        case_path,
        ("case_id", "category", "algorithm", "mode", "pair_id", "score_cutoff", "minimum_tier", "runtimes"),
        cases,
    )

    batch_random = random.Random(seed)
    choices = [random_text(batch_random, 8) for _ in range(10000)]
    queries = choices[::100]
    choice_path = output / "choices.txt"
    query_path = output / "queries.txt"
    choice_path.write_text("\n".join(choices) + "\n", encoding="utf-8", newline="\n")
    query_path.write_text("\n".join(queries) + "\n", encoding="utf-8", newline="\n")

    generated_paths = (case_path, pair_path, query_path, choice_path)
    combined = hashlib.sha256()
    for path in sorted(generated_paths, key=lambda value: value.name):
        combined.update(path.name.encode("utf-8"))
        combined.update(b"\0")
        combined.update(path.read_bytes())
    category_counts = Counter(case[1] for case in cases)
    tier_counts = Counter(case[6] for case in cases)
    metadata: dict[str, object] = {
        "schema_version": 1,
        "seed": seed,
        "pair_count": len(pair_rows),
        "case_count": len(cases),
        "query_count": len(queries),
        "choice_count": len(choices),
        "cell_count": len(queries) * len(choices),
        "category_counts": dict(sorted(category_counts.items())),
        "minimum_tier_counts": dict(sorted(tier_counts.items())),
        "files": {path.name: file_hash(path) for path in generated_paths},
        "corpus_sha256": combined.hexdigest(),
    }
    metadata_path = output / "metadata.json"
    metadata_path.write_text(json.dumps(metadata, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
    return metadata


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--seed", type=int, default=18)
    arguments = parser.parse_args()
    metadata = generate(arguments.output.resolve(), arguments.seed)
    print(json.dumps(metadata, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
