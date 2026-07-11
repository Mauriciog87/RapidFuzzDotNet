from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

import generate_corpus
import report
import validate_official_results


class CorpusTests(unittest.TestCase):
    def test_generation_is_deterministic_and_complete(self) -> None:
        with tempfile.TemporaryDirectory() as first_directory, tempfile.TemporaryDirectory() as second_directory:
            first = generate_corpus.generate(Path(first_directory), 18)
            second = generate_corpus.generate(Path(second_directory), 18)
            self.assertEqual(first["corpus_sha256"], second["corpus_sha256"])
            self.assertEqual(544, first["case_count"])
            self.assertEqual(64, first["pair_count"])
            self.assertEqual(1_000_000, first["cell_count"])
            expected, cell_count, corpus_hash = report.read_manifest(Path(first_directory), "common")
            self.assertEqual(544, len(set().union(*expected.values())))
            self.assertEqual(1_000_000, cell_count)
            self.assertEqual(first["corpus_sha256"], corpus_hash)


class ParserTests(unittest.TestCase):
    def test_complete_smoke_pipeline_uses_consistent_runtime_labels(self) -> None:
        runtime_labels = {
            "cpp": ("cpp-gcc", "cpp-clang"),
            "dotnet": ("dotnet-net10", "dotnet-net8"),
            "python": ("python",),
        }
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            corpus = root / "corpus"
            generate_corpus.generate(corpus, 18)
            expected, cell_count, corpus_hash = report.read_manifest(corpus, "smoke")
            results = []
            validations = {}
            for family, labels in runtime_labels.items():
                for label in labels:
                    for case_id in expected[family]:
                        results.append(report.create_result(case_id, label, [9.0, 10.0, 11.0], cell_count, None, root))
                        if case_id.startswith("batch/"):
                            validations[(label, case_id)] = {
                                "case_id": case_id,
                                "digest": "synthetic-digest",
                                "count": cell_count,
                            }
                        else:
                            validations[(label, case_id)] = {"case_id": case_id, "value": 100.0}
            indexed = report.index_results(results)
            report.validate_inventory(indexed, expected)
            report.validate_correctness(validations, expected)
            rows = report.enrich_rows(results)
            summaries = report.geometric_summary(rows)
            csv_path = root / "results.csv"
            html_path = root / "report.html"
            report.write_csv(csv_path, rows)
            report.write_html(
                html_path,
                rows,
                summaries,
                {"corpus_sha256": corpus_hash, "tier": "smoke", "hardware": {}},
            )
            table = report.readme_promotion_table(
                summaries,
                rows,
                {"tier": "smoke", "hardware": {}},
                {"rapidfuzz_cpp": "a" * 40, "rapidfuzz_python": "b" * 40},
            )
            csv_text = csv_path.read_text(encoding="utf-8")
            html_text = html_path.read_text(encoding="utf-8")
        expected_count = sum(len(expected[family]) * len(runtime_labels[family]) for family in runtime_labels)
        self.assertEqual(expected_count, len(rows))
        self.assertTrue(csv_text.startswith("case_id,runtime"))
        self.assertIn("RapidFuzz cross-language benchmark comparison", html_text)
        self.assertIn("Geomean vs best C++", table)
        self.assertIn("Cases where .NET wins", table)

    def test_google_parser_normalizes_units(self) -> None:
        document = {
            "benchmarks": [
                {
                    "name": "core/levenshtein/static/8/similar",
                    "run_name": "core/levenshtein/static/8/similar",
                    "run_type": "iteration",
                    "real_time": 2.5,
                    "time_unit": "us",
                },
                {
                    "name": "core/levenshtein/static/8/similar_median",
                    "run_name": "core/levenshtein/static/8/similar",
                    "run_type": "aggregate",
                    "aggregate_name": "median",
                    "real_time": 2.5,
                    "time_unit": "us",
                },
            ]
        }
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "google.json"
            path.write_text(json.dumps(document), encoding="utf-8")
            results = report.parse_google(path, "cpp-gcc", 1_000_000)
        self.assertEqual(1, len(results))
        self.assertEqual(2_500.0, results[0].median_ns)

    def test_dotnet_parser_extracts_full_case_identifier(self) -> None:
        document = {
            "Benchmarks": [
                {
                    "FullName": "Benchmarks.Execute(Case: core/jaro/static/64/similar)",
                    "Statistics": {"OriginalValues": [10.0, 12.0, 11.0]},
                    "Memory": {"BytesAllocatedPerOperation": 32},
                }
            ]
        }
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "dotnet-report-full.json"
            path.write_text(json.dumps(document), encoding="utf-8")
            results = report.parse_dotnet(path, "dotnet-net10", 1_000_000)
        self.assertEqual("core/jaro/static/64/similar", results[0].case_id)
        self.assertEqual(11.0, results[0].median_ns)
        self.assertEqual(32.0, results[0].allocated_bytes)

    def test_pyperf_parser_reads_process_values(self) -> None:
        document = {
            "benchmarks": [
                {
                    "metadata": {"name": "batch/ratio"},
                    "runs": [{"values": [0.001, 0.002]}],
                }
            ]
        }
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "python.json"
            path.write_text(json.dumps(document), encoding="utf-8")
            results = report.parse_pyperf(path, "python", 1_000_000)
        self.assertEqual(1.5, results[0].median_ns)
        self.assertEqual(1_000_000_000.0 / 1.5, results[0].cells_per_second)

    def test_duplicate_results_fail(self) -> None:
        result = report.create_result(
            "core/ratio/static/8/similar",
            "python",
            [10.0],
            1_000_000,
            None,
            Path("source.json"),
        )
        with self.assertRaises(ValueError):
            report.index_results([result, result])

    def test_unstable_measurements_do_not_influence_speedups(self) -> None:
        case_id = "core/ratio/static/8/similar"
        source = Path("source.json")
        results = [
            report.create_result(case_id, "cpp-gcc", [10.0, 10.0, 10.0], 1, None, source),
            report.create_result(case_id, "cpp-clang", [1.0, 100.0], 1, None, source),
            report.create_result(case_id, "dotnet-net10", [5.0, 5.0, 5.0], 1, None, source),
            report.create_result(case_id, "python", [20.0, 20.0, 20.0], 1, None, source),
        ]
        rows = report.enrich_rows(results)
        dotnet = next(row for row in rows if row["runtime"] == "dotnet-net10")
        self.assertEqual(2.0, dotnet["speedup_vs_cpp"])
        self.assertEqual(4.0, dotnet["speedup_vs_python"])
        unstable_dotnet = report.create_result(case_id, "dotnet-net10", [1.0, 100.0], 1, None, source)
        replacement = [result for result in results if result.runtime != "dotnet-net10"] + [unstable_dotnet]
        unstable_rows = report.enrich_rows(replacement)
        unstable = next(row for row in unstable_rows if row["runtime"] == "dotnet-net10")
        self.assertIsNone(unstable["speedup_vs_cpp"])
        self.assertIsNone(unstable["speedup_vs_python"])


class OfficialResultTests(unittest.TestCase):
    def test_complete_official_result_inventory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for compiler in ("gcc", "clang"):
                compiler_directory = root / f"cpp-{compiler}"
                compiler_directory.mkdir(parents=True)
                for executable, count in validate_official_results.CPP_EXECUTABLES.items():
                    benchmarks = [
                        {
                            "name": f"{executable}/{index}",
                            "run_name": f"{executable}/{index}",
                            "run_type": "iteration",
                        }
                        for index in range(count)
                    ]
                    (compiler_directory / f"{executable}.json").write_text(
                        json.dumps({"benchmarks": benchmarks}),
                        encoding="utf-8",
                    )
            for runtime in ("dotnet-net8", "dotnet-net10"):
                runtime_directory = root / runtime
                runtime_directory.mkdir()
                benchmarks = []
                for class_name, count in validate_official_results.DOTNET_CLASSES.items():
                    benchmarks.extend(
                        {
                            "FullName": f"RapidFuzzDotNet.Benchmarks.{class_name}.Execute(Case: {index})",
                        }
                        for index in range(count)
                    )
                (runtime_directory / "official-report-full.json").write_text(
                    json.dumps({"Benchmarks": benchmarks}),
                    encoding="utf-8",
                )
            python_results = [{"scorer": scorer} for scorer in validate_official_results.PYTHON_SCORERS]
            (root / "python-corrected.json").write_text(
                json.dumps(
                    {
                        "corpus_sha256": "synthetic-corpus",
                        "query_count": 100,
                        "choice_count": 10000,
                        "cell_count": 1000000,
                        "workers": 1,
                        "results": python_results,
                    }
                ),
                encoding="utf-8",
            )
            (root / "python-original.log").write_text("captured output\n", encoding="utf-8")
            (root / "python-original.exit-code").write_text("1\n", encoding="utf-8")
            result = validate_official_results.validate(root, "synthetic-corpus")
        self.assertEqual(77, result["cpp_registration_count"])
        self.assertEqual(108, result["expanded_workload_count"])
        self.assertEqual(8, result["python_scorer_count"])


if __name__ == "__main__":
    unittest.main()
