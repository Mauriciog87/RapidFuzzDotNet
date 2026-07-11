from __future__ import annotations

import argparse
import csv
import html
import json
import math
import re
import statistics
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Iterable


TIER_RANKS = {"smoke": 0, "common": 1, "all": 2}
TIME_FACTORS = {"ns": 1.0, "us": 1_000.0, "ms": 1_000_000.0, "s": 1_000_000_000.0}
INTEGER_ALGORITHMS = {"levenshtein", "indel", "lcs_seq"}


@dataclass(frozen=True)
class TimingResult:
    case_id: str
    runtime: str
    category: str
    algorithm: str
    mode: str
    samples_ns: tuple[float, ...]
    median_ns: float
    standard_deviation_ns: float
    coefficient_of_variation: float
    stable: bool
    operations_per_second: float
    cells_per_second: float | None
    allocated_bytes: float | None
    source_file: str


def split_assignment(value: str) -> tuple[str, Path]:
    label, separator, raw_path = value.partition("=")
    if not separator or not label or not raw_path:
        raise ValueError(f"Expected LABEL=PATH, received '{value}'.")
    return label, Path(raw_path).resolve()


def case_parts(case_id: str) -> tuple[str, str, str]:
    parts = case_id.split("/")
    if len(parts) < 2:
        raise ValueError(f"Invalid benchmark case identifier '{case_id}'.")
    if parts[0] == "batch":
        return "batch", parts[1], "static"
    if len(parts) < 4:
        raise ValueError(f"Invalid benchmark case identifier '{case_id}'.")
    return parts[0], parts[1], parts[2]


def create_result(
    case_id: str,
    runtime: str,
    samples_ns: Iterable[float],
    cell_count: int,
    allocated_bytes: float | None,
    source_file: Path,
) -> TimingResult:
    values = tuple(float(value) for value in samples_ns)
    if not values or any(not math.isfinite(value) or value <= 0 for value in values):
        raise ValueError(f"Benchmark '{case_id}' for '{runtime}' has invalid samples.")
    category, algorithm, mode = case_parts(case_id)
    divisor = cell_count if category == "batch" else 1
    normalized = tuple(value / divisor for value in values)
    median_ns = statistics.median(normalized)
    mean_ns = statistics.fmean(normalized)
    deviation = statistics.pstdev(normalized) if len(normalized) > 1 else 0.0
    coefficient = deviation / mean_ns if mean_ns > 0 else math.inf
    cells_per_second = 1_000_000_000.0 / median_ns if category == "batch" else None
    return TimingResult(
        case_id,
        runtime,
        category,
        algorithm,
        mode,
        normalized,
        median_ns,
        deviation,
        coefficient,
        coefficient <= 0.1,
        1_000_000_000.0 / median_ns,
        cells_per_second,
        allocated_bytes,
        str(source_file),
    )


def parse_google(path: Path, runtime: str, cell_count: int) -> list[TimingResult]:
    document = json.loads(path.read_text(encoding="utf-8"))
    grouped: dict[str, list[float]] = {}
    for entry in document.get("benchmarks", []):
        if entry.get("run_type") == "aggregate":
            continue
        name = entry.get("run_name") or entry.get("name")
        unit = entry.get("time_unit")
        if not isinstance(name, str) or unit not in TIME_FACTORS:
            raise ValueError(f"Unknown Google Benchmark name or unit in '{path}'.")
        real_time = entry.get("real_time")
        if not isinstance(real_time, (int, float)):
            raise ValueError(f"Missing Google Benchmark timing in '{path}'.")
        grouped.setdefault(name, []).append(float(real_time) * TIME_FACTORS[unit])
    return [create_result(case_id, runtime, values, cell_count, None, path) for case_id, values in grouped.items()]


def extract_benchmark_case(full_name: str) -> str:
    match = re.search(r"(?:Case|Registration): ([^)]+)", full_name)
    if match is None:
        raise ValueError(f"BenchmarkDotNet result lacks a case identifier: '{full_name}'.")
    return match.group(1)


def parse_dotnet(path: Path, runtime: str, cell_count: int) -> list[TimingResult]:
    document = json.loads(path.read_text(encoding="utf-8"))
    results: list[TimingResult] = []
    for benchmark in document.get("Benchmarks", []):
        full_name = benchmark.get("FullName")
        if not isinstance(full_name, str):
            raise ValueError(f"BenchmarkDotNet result lacks FullName in '{path}'.")
        case_id = extract_benchmark_case(full_name)
        statistics_data = benchmark.get("Statistics") or {}
        samples = statistics_data.get("OriginalValues")
        if not isinstance(samples, list) or not samples:
            measurements = benchmark.get("Measurements") or []
            samples = [
                float(measurement["Nanoseconds"]) / float(measurement["Operations"])
                for measurement in measurements
                if measurement.get("IterationStage") == "Result" and measurement.get("Operations", 0) > 0
            ]
        allocated: float | None = None
        memory = benchmark.get("Memory")
        if isinstance(memory, dict) and isinstance(memory.get("BytesAllocatedPerOperation"), (int, float)):
            allocated = float(memory["BytesAllocatedPerOperation"])
        results.append(create_result(case_id, runtime, samples, cell_count, allocated, path))
    return results


def parse_dotnet_directory(directory: Path, runtime: str, cell_count: int) -> list[TimingResult]:
    paths = sorted(directory.rglob("*-report-full.json"))
    if not paths:
        raise ValueError(f"No BenchmarkDotNet full JSON reports were found in '{directory}'.")
    results: list[TimingResult] = []
    for path in paths:
        results.extend(parse_dotnet(path, runtime, cell_count))
    return results


def parse_pyperf(path: Path, runtime: str, cell_count: int) -> list[TimingResult]:
    document = json.loads(path.read_text(encoding="utf-8"))
    results: list[TimingResult] = []
    for benchmark in document.get("benchmarks", []):
        metadata = benchmark.get("metadata") or {}
        name = metadata.get("name") or benchmark.get("name")
        if not isinstance(name, str):
            raise ValueError(f"pyperf result lacks a benchmark name in '{path}'.")
        values: list[float] = []
        for run in benchmark.get("runs", []):
            run_values = run.get("values")
            if isinstance(run_values, list):
                values.extend(float(value) * 1_000_000_000.0 for value in run_values)
        results.append(create_result(name, runtime, values, cell_count, None, path))
    return results


def read_manifest(directory: Path, tier: str) -> tuple[dict[str, set[str]], int, str]:
    metadata = json.loads((directory / "metadata.json").read_text(encoding="utf-8"))
    selected_rank = TIER_RANKS[tier]
    expected: dict[str, set[str]] = {"cpp": set(), "dotnet": set(), "python": set()}
    with (directory / "cases.tsv").open(encoding="utf-8", newline="") as stream:
        for row in csv.DictReader(stream, delimiter="\t"):
            if TIER_RANKS[row["minimum_tier"]] > selected_rank:
                continue
            for runtime in row["runtimes"].split(","):
                expected[runtime].add(row["case_id"])
    return expected, int(metadata["cell_count"]), str(metadata["corpus_sha256"])


def index_results(results: Iterable[TimingResult]) -> dict[tuple[str, str], TimingResult]:
    indexed: dict[tuple[str, str], TimingResult] = {}
    for result in results:
        key = (result.runtime, result.case_id)
        if key in indexed:
            raise ValueError(f"Duplicate timing result for runtime '{result.runtime}' and case '{result.case_id}'.")
        indexed[key] = result
    return indexed


def validate_inventory(indexed: dict[tuple[str, str], TimingResult], expected: dict[str, set[str]]) -> None:
    required_labels = {
        "cpp": ("cpp-gcc", "cpp-clang"),
        "dotnet": ("dotnet-net10", "dotnet-net8"),
        "python": ("python",),
    }
    for family, labels in required_labels.items():
        for label in labels:
            actual = {case_id for runtime, case_id in indexed if runtime == label}
            missing = expected[family] - actual
            extra = actual - expected[family]
            if missing or extra:
                raise ValueError(
                    f"Timing inventory mismatch for '{label}': missing={sorted(missing)}, extra={sorted(extra)}."
                )


def read_validations(paths: list[Path], expected_sha: str) -> dict[tuple[str, str], dict[str, object]]:
    indexed: dict[tuple[str, str], dict[str, object]] = {}
    for path in paths:
        document = json.loads(path.read_text(encoding="utf-8"))
        if document.get("corpus_sha256") != expected_sha:
            raise ValueError(f"Validation corpus SHA mismatch in '{path}'.")
        runtime = document.get("runtime")
        if not isinstance(runtime, str):
            raise ValueError(f"Validation runtime is missing in '{path}'.")
        for result in document.get("results", []):
            case_id = result.get("case_id")
            if not isinstance(case_id, str):
                raise ValueError(f"Validation case is missing in '{path}'.")
            key = (runtime, case_id)
            if key in indexed:
                raise ValueError(f"Duplicate validation result for '{runtime}' and '{case_id}'.")
            indexed[key] = result
    return indexed


def validate_correctness(
    validations: dict[tuple[str, str], dict[str, object]],
    expected: dict[str, set[str]],
) -> None:
    labels = {
        "cpp": ("cpp-gcc", "cpp-clang"),
        "dotnet": ("dotnet-net10", "dotnet-net8"),
        "python": ("python",),
    }
    for family, runtime_labels in labels.items():
        for runtime in runtime_labels:
            actual = {case_id for label, case_id in validations if label == runtime}
            missing = expected[family] - actual
            extra = actual - expected[family]
            if missing or extra:
                raise ValueError(
                    f"Validation inventory mismatch for '{runtime}': missing={sorted(missing)}, extra={sorted(extra)}."
                )
    all_cases = sorted(set().union(*expected.values()))
    for case_id in all_cases:
        candidates = [value for (runtime, current_case), value in validations.items() if current_case == case_id]
        if not candidates:
            raise ValueError(f"No correctness values exist for '{case_id}'.")
        if case_id.startswith("batch/"):
            digests = {candidate.get("digest") for candidate in candidates}
            counts = {candidate.get("count") for candidate in candidates}
            if len(digests) != 1 or None in digests or len(counts) != 1:
                raise ValueError(f"Batch correctness mismatch for '{case_id}'.")
            continue
        values = [candidate.get("value") for candidate in candidates]
        if any(not isinstance(value, (int, float)) for value in values):
            raise ValueError(f"Missing correctness score for '{case_id}'.")
        algorithm = case_parts(case_id)[1]
        reference = float(values[0])
        for value in values[1:]:
            current = float(value)
            if algorithm in INTEGER_ALGORITHMS:
                if current != reference:
                    raise ValueError(f"Integer correctness mismatch for '{case_id}'.")
            elif not math.isclose(current, reference, rel_tol=0.0, abs_tol=1e-12):
                raise ValueError(f"Floating-point correctness mismatch for '{case_id}'.")


def enrich_rows(results: list[TimingResult]) -> list[dict[str, object]]:
    indexed = index_results(results)
    rows: list[dict[str, object]] = []
    for result in sorted(results, key=lambda value: (value.case_id, value.runtime)):
        cpp_candidates = [
            indexed[(runtime, result.case_id)]
            for runtime in ("cpp-gcc", "cpp-clang")
            if (runtime, result.case_id) in indexed and indexed[(runtime, result.case_id)].stable
        ]
        python_result = indexed.get(("python", result.case_id))
        row = asdict(result)
        row["samples_ns"] = list(result.samples_ns)
        row["speedup_vs_cpp"] = (
            min(candidate.median_ns for candidate in cpp_candidates) / result.median_ns
            if result.stable and cpp_candidates
            else None
        )
        row["speedup_vs_python"] = (
            python_result.median_ns / result.median_ns
            if result.stable and python_result is not None and python_result.stable
            else None
        )
        rows.append(row)
    return rows


def geometric_summary(rows: list[dict[str, object]]) -> list[dict[str, object]]:
    summaries: list[dict[str, object]] = []
    categories = sorted({str(row["category"]) for row in rows})
    for category in categories:
        dotnet_rows = [
            row
            for row in rows
            if row["runtime"] == "dotnet-net10" and row["category"] == category
        ]
        cpp_values = [float(row["speedup_vs_cpp"]) for row in dotnet_rows if row["speedup_vs_cpp"] is not None]
        python_values = [float(row["speedup_vs_python"]) for row in dotnet_rows if row["speedup_vs_python"] is not None]
        summaries.append(
            {
                "category": category,
                "case_count": len(dotnet_rows),
                "stable_case_count": sum(bool(row["stable"]) for row in dotnet_rows),
                "dotnet_wins_vs_cpp": sum(value > 1.0 for value in cpp_values),
                "dotnet_losses_vs_cpp": sum(value < 1.0 for value in cpp_values),
                "geomean_speedup_vs_cpp": statistics.geometric_mean(cpp_values) if cpp_values else None,
                "geomean_speedup_vs_python": statistics.geometric_mean(python_values) if python_values else None,
            }
        )
    return summaries


def write_csv(path: Path, rows: list[dict[str, object]]) -> None:
    fieldnames = [
        "case_id",
        "runtime",
        "category",
        "algorithm",
        "mode",
        "median_ns",
        "standard_deviation_ns",
        "coefficient_of_variation",
        "stable",
        "operations_per_second",
        "cells_per_second",
        "allocated_bytes",
        "speedup_vs_cpp",
        "speedup_vs_python",
        "source_file",
    ]
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=fieldnames, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)


def format_ratio(value: object) -> str:
    return "n/a" if value is None else f"{float(value):.3f}x"


def summary_table(summaries: list[dict[str, object]]) -> str:
    lines = [
        "| Category | Cases | Stable | .NET wins vs best C++ | .NET losses vs best C++ | Geomean vs best C++ | Geomean vs Python |",
        "| --- | ---: | ---: | ---: | ---: | ---: | ---: |",
    ]
    for summary in summaries:
        lines.append(
            f"| {summary['category']} | {summary['case_count']} | {summary['stable_case_count']} | "
            f"{summary['dotnet_wins_vs_cpp']} | {summary['dotnet_losses_vs_cpp']} | "
            f"{format_ratio(summary['geomean_speedup_vs_cpp'])} | {format_ratio(summary['geomean_speedup_vs_python'])} |"
        )
    return "\n".join(lines)


def readme_promotion_table(
    summaries: list[dict[str, object]],
    rows: list[dict[str, object]],
    metadata: dict[str, object],
    versions: dict[str, str],
) -> str:
    hardware = metadata.get("hardware")
    hardware_values = hardware if isinstance(hardware, dict) else {}
    cpp_values = [float(value["geomean_speedup_vs_cpp"]) for value in summaries if value["geomean_speedup_vs_cpp"] is not None]
    python_values = [float(value["geomean_speedup_vs_python"]) for value in summaries if value["geomean_speedup_vs_python"] is not None]
    winners = [str(value["category"]) for value in summaries if value["geomean_speedup_vs_cpp"] is not None and float(value["geomean_speedup_vs_cpp"]) > 1.0]
    losses = [str(value["category"]) for value in summaries if value["geomean_speedup_vs_cpp"] is not None and float(value["geomean_speedup_vs_cpp"]) < 1.0]
    comparable = [
        value
        for value in rows
        if value["runtime"] == "dotnet-net10" and value["speedup_vs_cpp"] is not None
    ]
    winning_cases = sorted(
        (value for value in comparable if float(value["speedup_vs_cpp"]) > 1.0),
        key=lambda value: float(value["speedup_vs_cpp"]),
        reverse=True,
    )
    losing_cases = sorted(
        (value for value in comparable if float(value["speedup_vs_cpp"]) < 1.0),
        key=lambda value: float(value["speedup_vs_cpp"]),
    )

    def case_summary(values: list[dict[str, object]]) -> str:
        examples = ", ".join(f"`{value['case_id']}`" for value in values[:3])
        return f"{len(values)} cases: {examples}" if examples else "none"

    run_url = hardware_values.get("run_url") or "Artifact link pending"
    run_cell = f"[workflow run]({run_url})" if str(run_url).startswith("https://") else str(run_url)
    commit = str(hardware_values.get("repository_commit") or "unknown")[:8]
    commits = f"C++ `{versions['rapidfuzz_cpp'][:8]}`, Python `{versions['rapidfuzz_python'][:8]}`, .NET `{commit}`"
    category_text = f"wins: {', '.join(winners) or 'none'}; losses: {', '.join(losses) or 'none'}"
    lines = [
        "| Date | Workflow | Hardware | Commits | Category results | Cases where .NET wins | Cases where .NET loses | Geomean vs best C++ | Geomean vs Python | Full results |",
        "| --- | --- | --- | --- | --- | --- | --- | ---: | ---: | --- |",
        f"| {hardware_values.get('date', 'unknown')} | `{hardware_values.get('workflow', 'benchmarks')} / {metadata['tier']}` | "
        f"{hardware_values.get('processor', 'unknown')} | {commits} | {category_text} | {case_summary(winning_cases)} | {case_summary(losing_cases)} | "
        f"{format_ratio(statistics.geometric_mean(cpp_values) if cpp_values else None)} | "
        f"{format_ratio(statistics.geometric_mean(python_values) if python_values else None)} | {run_cell} |",
    ]
    return "\n".join(lines)


def write_html(path: Path, rows: list[dict[str, object]], summaries: list[dict[str, object]], metadata: dict[str, object]) -> None:
    table_rows = []
    for row in rows:
        table_rows.append(
            "<tr>"
            f"<td>{html.escape(str(row['case_id']))}</td>"
            f"<td>{html.escape(str(row['runtime']))}</td>"
            f"<td>{float(row['median_ns']):.3f}</td>"
            f"<td>{float(row['coefficient_of_variation']):.2%}</td>"
            f"<td>{format_ratio(row['speedup_vs_cpp'])}</td>"
            f"<td>{format_ratio(row['speedup_vs_python'])}</td>"
            "</tr>"
        )
    content = (
        "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>RapidFuzz benchmark comparison</title>"
        "<style>body{font-family:system-ui;margin:2rem;color:#171717}table{border-collapse:collapse;width:100%}"
        "th,td{border:1px solid #d4d4d4;padding:.45rem;text-align:right}th:first-child,td:first-child{text-align:left}"
        "thead{background:#f5f5f5}code{font-size:.9em}</style></head><body>"
        "<h1>RapidFuzz cross-language benchmark comparison</h1>"
        f"<p>Corpus <code>{html.escape(str(metadata['corpus_sha256']))}</code>. Values above 1.0x mean .NET was faster.</p>"
        f"<pre>{html.escape(summary_table(summaries))}</pre>"
        "<table><thead><tr><th>Case</th><th>Runtime</th><th>ns/op</th><th>CV</th><th>vs best C++</th><th>vs Python</th></tr></thead><tbody>"
        + "".join(table_rows)
        + "</tbody></table></body></html>"
    )
    path.write_text(content, encoding="utf-8", newline="\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--corpus", type=Path, required=True)
    parser.add_argument("--tier", choices=tuple(TIER_RANKS), required=True)
    parser.add_argument("--google", action="append", default=[])
    parser.add_argument("--dotnet", action="append", default=[])
    parser.add_argument("--pyperf", action="append", default=[])
    parser.add_argument("--validation", action="append", type=Path, default=[])
    parser.add_argument("--official-python", type=Path)
    parser.add_argument("--hardware", type=Path)
    parser.add_argument("--versions", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    arguments = parser.parse_args()
    corpus = arguments.corpus.resolve()
    expected, cell_count, corpus_sha = read_manifest(corpus, arguments.tier)
    timings: list[TimingResult] = []
    for assignment in arguments.google:
        label, path = split_assignment(assignment)
        timings.extend(parse_google(path, label, cell_count))
    for assignment in arguments.dotnet:
        label, path = split_assignment(assignment)
        timings.extend(parse_dotnet_directory(path, label, cell_count))
    for assignment in arguments.pyperf:
        label, path = split_assignment(assignment)
        timings.extend(parse_pyperf(path, label, cell_count))
    indexed = index_results(timings)
    validate_inventory(indexed, expected)
    validations = read_validations([path.resolve() for path in arguments.validation], corpus_sha)
    validate_correctness(validations, expected)
    rows = enrich_rows(timings)
    summaries = geometric_summary(rows)
    metadata: dict[str, object] = {
        "corpus_sha256": corpus_sha,
        "tier": arguments.tier,
        "cell_count": cell_count,
        "unstable_case_count": sum(not result.stable for result in timings),
    }
    if arguments.hardware:
        metadata["hardware"] = json.loads(arguments.hardware.resolve().read_text(encoding="utf-8"))
    versions = json.loads(arguments.versions.resolve().read_text(encoding="utf-8"))
    metadata["versions"] = versions
    official_python: dict[str, object] | None = None
    if arguments.official_python:
        official_python = json.loads(arguments.official_python.resolve().read_text(encoding="utf-8"))
        if official_python.get("corpus_sha256") != corpus_sha:
            raise ValueError("Official Python benchmark corpus SHA does not match the common suite.")
    output = arguments.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    document = {
        "metadata": metadata,
        "summaries": summaries,
        "results": rows,
        "official_python": official_python,
    }
    (output / "summary.json").write_text(json.dumps(document, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
    write_csv(output / "results.csv", rows)
    write_html(output / "report.html", rows, summaries, metadata)
    table = summary_table(summaries)
    warning = "Results with coefficient of variation above 10% are marked unstable and must not be promoted."
    (output / "job-summary.md").write_text(f"## Cross-language benchmark results\n\n{table}\n\n{warning}\n", encoding="utf-8", newline="\n")
    (output / "readme-table.md").write_text(
        readme_promotion_table(summaries, rows, metadata, versions) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
