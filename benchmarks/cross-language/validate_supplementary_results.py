from __future__ import annotations

import argparse
import json
from pathlib import Path


RUNTIMES = ("dotnet-net8", "dotnet-net10")
EXPECTED_WORKLOAD_COUNT = 268


def validate(root: Path) -> dict[str, object]:
    runtime_counts: dict[str, int] = {}
    for runtime in RUNTIMES:
        result_directory = root / runtime / "results"
        paths = sorted(result_directory.glob("*-report-full.json"))
        if len(paths) != 1:
            raise ValueError(f"Expected one full JSON report for '{runtime}', found {len(paths)}.")
        document = json.loads(paths[0].read_text(encoding="utf-8"))
        benchmarks = document.get("Benchmarks")
        if not isinstance(benchmarks, list):
            raise ValueError(f"Supplementary report for '{runtime}' does not contain benchmarks.")
        names = [benchmark.get("FullName") for benchmark in benchmarks]
        if any(not isinstance(name, str) or not name for name in names):
            raise ValueError(f"Supplementary report for '{runtime}' contains an unnamed benchmark.")
        if len(names) != len(set(names)):
            raise ValueError(f"Supplementary report for '{runtime}' contains duplicate benchmarks.")
        if len(names) != EXPECTED_WORKLOAD_COUNT:
            raise ValueError(
                f"Supplementary report for '{runtime}' contains {len(names)} workloads; expected {EXPECTED_WORKLOAD_COUNT}."
            )
        runtime_counts[runtime] = len(names)
    return {"expected_workload_count": EXPECTED_WORKLOAD_COUNT, "runtimes": runtime_counts}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    arguments = parser.parse_args()
    print(json.dumps(validate(arguments.root.resolve()), sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
