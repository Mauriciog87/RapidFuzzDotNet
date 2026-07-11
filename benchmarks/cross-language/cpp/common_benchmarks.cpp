#include <benchmark/benchmark.h>
#include <rapidfuzz/distance/Indel.hpp>
#include <rapidfuzz/distance/Jaro.hpp>
#include <rapidfuzz/distance/JaroWinkler.hpp>
#include <rapidfuzz/distance/LCSseq.hpp>
#include <rapidfuzz/distance/Levenshtein.hpp>
#include <rapidfuzz/fuzz.hpp>

#include <cmath>
#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <limits>
#include <map>
#include <memory>
#include <sstream>
#include <stdexcept>
#include <string>
#include <utility>
#include <vector>

namespace {

constexpr std::uint64_t FnvOffset = 14695981039346656037ULL;
constexpr std::uint64_t FnvPrime = 1099511628211ULL;
constexpr double QuantizationScale = 1000000000000.0;

struct BenchmarkCase {
    std::string id;
    std::string category;
    std::string algorithm;
    std::string mode;
    std::string source;
    std::string target;
    double score_cutoff;
};

struct ValidationResult {
    std::string case_id;
    bool batch;
    double value;
    std::string digest;
    std::size_t count;
    std::vector<double> samples;
};

std::vector<std::string> split(const std::string& value, char separator)
{
    std::vector<std::string> values;
    std::string current;
    std::istringstream stream(value);
    while (std::getline(stream, current, separator)) values.push_back(current);
    if (!value.empty() && value.back() == separator) values.emplace_back();
    return values;
}

std::string require_environment(const char* name)
{
    const char* value = std::getenv(name);
    if (value == nullptr || *value == '\0') throw std::runtime_error(std::string(name) + " is required");
    return value;
}

int tier_rank(const std::string& tier)
{
    if (tier == "smoke") return 0;
    if (tier == "common") return 1;
    if (tier == "all") return 2;
    throw std::runtime_error("unknown benchmark tier");
}

bool contains_runtime(const std::string& runtimes, const std::string& runtime)
{
    std::vector<std::string> values = split(runtimes, ',');
    for (const std::string& value : values) {
        if (value == runtime) return true;
    }
    return false;
}

std::map<std::string, std::pair<std::string, std::string>> read_pairs(const std::string& directory)
{
    std::ifstream stream(directory + "/pairs.tsv");
    if (!stream) throw std::runtime_error("unable to open pairs.tsv");
    std::map<std::string, std::pair<std::string, std::string>> pairs;
    std::string line;
    std::getline(stream, line);
    while (std::getline(stream, line)) {
        std::vector<std::string> fields = split(line, '\t');
        if (fields.size() != 5 || !pairs.emplace(fields[0], std::make_pair(fields[3], fields[4])).second)
            throw std::runtime_error("invalid or duplicate benchmark pair");
    }
    return pairs;
}

std::vector<BenchmarkCase> read_cases(const std::string& directory, const std::string& tier)
{
    std::map<std::string, std::pair<std::string, std::string>> pairs = read_pairs(directory);
    std::ifstream stream(directory + "/cases.tsv");
    if (!stream) throw std::runtime_error("unable to open cases.tsv");
    std::vector<BenchmarkCase> cases;
    std::string line;
    std::getline(stream, line);
    int selected_rank = tier_rank(tier);
    while (std::getline(stream, line)) {
        std::vector<std::string> fields = split(line, '\t');
        if (fields.size() != 8) throw std::runtime_error("invalid benchmark case");
        if (!contains_runtime(fields[7], "cpp") || tier_rank(fields[6]) > selected_rank) continue;
        std::string source;
        std::string target;
        if (!fields[4].empty()) {
            auto pair = pairs.find(fields[4]);
            if (pair == pairs.end()) throw std::runtime_error("unknown benchmark pair");
            source = pair->second.first;
            target = pair->second.second;
        }
        cases.push_back({fields[0], fields[1], fields[2], fields[3], source, target, std::stod(fields[5])});
    }
    return cases;
}

std::vector<std::string> read_lines(const std::string& path)
{
    std::ifstream stream(path);
    if (!stream) throw std::runtime_error("unable to open benchmark corpus list");
    std::vector<std::string> values;
    std::string line;
    while (std::getline(stream, line)) values.push_back(line);
    return values;
}

std::string json_escape(const std::string& value)
{
    std::ostringstream output;
    for (char character : value) {
        switch (character) {
        case '"': output << "\\\""; break;
        case '\\': output << "\\\\"; break;
        case '\b': output << "\\b"; break;
        case '\f': output << "\\f"; break;
        case '\n': output << "\\n"; break;
        case '\r': output << "\\r"; break;
        case '\t': output << "\\t"; break;
        default: output << character; break;
        }
    }
    return output.str();
}

void update_digest(std::uint64_t& hash, double value)
{
    std::int64_t quantized = static_cast<std::int64_t>(std::llround(value * QuantizationScale));
    std::uint64_t bits = static_cast<std::uint64_t>(quantized);
    for (int index = 0; index < 8; ++index) {
        hash ^= static_cast<std::uint8_t>(bits >> (index * 8));
        hash *= FnvPrime;
    }
}

std::string format_digest(std::uint64_t hash)
{
    std::ostringstream output;
    output << std::hex << std::setfill('0') << std::setw(16) << hash;
    return output.str();
}

template <typename Function>
void register_individual(const BenchmarkCase& benchmark_case, Function function, std::vector<ValidationResult>& validations)
{
    double validation_value = static_cast<double>(function());
    validations.push_back({benchmark_case.id, false, validation_value, std::string(), 1, {validation_value}});
    benchmark::RegisterBenchmark(benchmark_case.id.c_str(), [function](benchmark::State& state) {
        for (auto unused : state) {
            static_cast<void>(unused);
            auto result = function();
            benchmark::DoNotOptimize(result);
        }
    });
}

template <typename Function>
void register_batch(
    const BenchmarkCase& benchmark_case,
    const std::shared_ptr<const std::vector<std::string>>& queries,
    const std::shared_ptr<const std::vector<std::string>>& choices,
    Function function,
    std::vector<ValidationResult>& validations)
{
    std::size_t count = queries->size() * choices->size();
    std::size_t middle = count / 2;
    std::size_t flat_index = 0;
    std::uint64_t hash = FnvOffset;
    std::vector<double> samples(3);
    for (const std::string& query : *queries) {
        for (const std::string& choice : *choices) {
            double value = static_cast<double>(function(query, choice));
            if (flat_index == 0) samples[0] = value;
            if (flat_index == middle) samples[1] = value;
            if (flat_index + 1 == count) samples[2] = value;
            update_digest(hash, value);
            ++flat_index;
        }
    }
    validations.push_back({benchmark_case.id, true, 0.0, format_digest(hash), count, samples});
    benchmark::RegisterBenchmark(benchmark_case.id.c_str(), [queries, choices, function, count](benchmark::State& state) {
        for (auto unused : state) {
            static_cast<void>(unused);
            std::vector<double> results;
            results.reserve(count);
            for (const std::string& query : *queries) {
                for (const std::string& choice : *choices)
                    results.push_back(static_cast<double>(function(query, choice)));
            }
            benchmark::DoNotOptimize(results.data());
            benchmark::ClobberMemory();
        }
        state.SetItemsProcessed(state.iterations() * static_cast<std::int64_t>(count));
    });
}

void register_core(const BenchmarkCase& value, std::vector<ValidationResult>& validations)
{
    const std::string source = value.source;
    const std::string target = value.target;
    if (value.algorithm == "levenshtein") {
        if (value.mode == "cached") {
            auto scorer = std::make_shared<rapidfuzz::CachedLevenshtein<char>>(source);
            register_individual(value, [scorer, target] { return scorer->distance(target); }, validations);
        }
        else if (value.mode == "cutoff") {
            std::size_t cutoff = static_cast<std::size_t>(value.score_cutoff);
            register_individual(value, [source, target, cutoff] { return rapidfuzz::levenshtein_distance(source, target, {1, 1, 1}, cutoff); }, validations);
        }
        else register_individual(value, [source, target] { return rapidfuzz::levenshtein_distance(source, target); }, validations);
    }
    else if (value.algorithm == "indel") {
        if (value.mode == "cached") {
            auto scorer = std::make_shared<rapidfuzz::CachedIndel<char>>(source);
            register_individual(value, [scorer, target] { return scorer->distance(target); }, validations);
        }
        else if (value.mode == "cutoff") {
            std::size_t cutoff = static_cast<std::size_t>(value.score_cutoff);
            register_individual(value, [source, target, cutoff] { return rapidfuzz::indel_distance(source, target, cutoff); }, validations);
        }
        else register_individual(value, [source, target] { return rapidfuzz::indel_distance(source, target); }, validations);
    }
    else if (value.algorithm == "lcs_seq") {
        if (value.mode == "cached") {
            auto scorer = std::make_shared<rapidfuzz::CachedLCSseq<char>>(source);
            register_individual(value, [scorer, target] { return scorer->similarity(target); }, validations);
        }
        else if (value.mode == "cutoff") {
            std::size_t cutoff = static_cast<std::size_t>(value.score_cutoff);
            register_individual(value, [source, target, cutoff] { return rapidfuzz::lcs_seq_similarity(source, target, cutoff); }, validations);
        }
        else register_individual(value, [source, target] { return rapidfuzz::lcs_seq_similarity(source, target); }, validations);
    }
    else if (value.algorithm == "jaro") {
        if (value.mode == "cached") {
            auto scorer = std::make_shared<rapidfuzz::CachedJaro<char>>(source);
            register_individual(value, [scorer, target] { return scorer->similarity(target); }, validations);
        }
        else if (value.mode == "cutoff") {
            double cutoff = value.score_cutoff;
            register_individual(value, [source, target, cutoff] { return rapidfuzz::jaro_similarity(source, target, cutoff); }, validations);
        }
        else register_individual(value, [source, target] { return rapidfuzz::jaro_similarity(source, target); }, validations);
    }
    else if (value.algorithm == "jaro_winkler") {
        if (value.mode == "cached") {
            auto scorer = std::make_shared<rapidfuzz::CachedJaroWinkler<char>>(source);
            register_individual(value, [scorer, target] { return scorer->similarity(target); }, validations);
        }
        else if (value.mode == "cutoff") {
            double cutoff = value.score_cutoff;
            register_individual(value, [source, target, cutoff] { return rapidfuzz::jaro_winkler_similarity(source, target, 0.1, cutoff); }, validations);
        }
        else register_individual(value, [source, target] { return rapidfuzz::jaro_winkler_similarity(source, target); }, validations);
    }
    else if (value.algorithm == "ratio") {
        if (value.mode == "cached") {
            auto scorer = std::make_shared<rapidfuzz::fuzz::CachedRatio<char>>(source);
            register_individual(value, [scorer, target] { return scorer->similarity(target); }, validations);
        }
        else if (value.mode == "cutoff") {
            double cutoff = value.score_cutoff;
            register_individual(value, [source, target, cutoff] { return rapidfuzz::fuzz::ratio(source, target, cutoff); }, validations);
        }
        else register_individual(value, [source, target] { return rapidfuzz::fuzz::ratio(source, target); }, validations);
    }
    else throw std::runtime_error("unknown core benchmark algorithm");
}

void register_fuzz(const BenchmarkCase& value, std::vector<ValidationResult>& validations)
{
    const std::string source = value.source;
    const std::string target = value.target;
    const double cutoff = value.mode == "cutoff" ? value.score_cutoff : 0.0;
    if (value.algorithm == "ratio") register_individual(value, [source, target, cutoff] { return rapidfuzz::fuzz::ratio(source, target, cutoff); }, validations);
    else if (value.algorithm == "partial_ratio") register_individual(value, [source, target, cutoff] { return rapidfuzz::fuzz::partial_ratio(source, target, cutoff); }, validations);
    else if (value.algorithm == "token_sort_ratio") register_individual(value, [source, target, cutoff] { return rapidfuzz::fuzz::token_sort_ratio(source, target, cutoff); }, validations);
    else if (value.algorithm == "token_set_ratio") register_individual(value, [source, target, cutoff] { return rapidfuzz::fuzz::token_set_ratio(source, target, cutoff); }, validations);
    else if (value.algorithm == "partial_token_sort_ratio") register_individual(value, [source, target, cutoff] { return rapidfuzz::fuzz::partial_token_sort_ratio(source, target, cutoff); }, validations);
    else if (value.algorithm == "partial_token_set_ratio") register_individual(value, [source, target, cutoff] { return rapidfuzz::fuzz::partial_token_set_ratio(source, target, cutoff); }, validations);
    else if (value.algorithm == "qratio") register_individual(value, [source, target, cutoff] { return rapidfuzz::fuzz::QRatio(source, target, cutoff); }, validations);
    else if (value.algorithm == "wratio") register_individual(value, [source, target, cutoff] { return rapidfuzz::fuzz::WRatio(source, target, cutoff); }, validations);
    else throw std::runtime_error("unknown fuzz benchmark algorithm");
}

void register_batch_case(
    const BenchmarkCase& value,
    const std::shared_ptr<const std::vector<std::string>>& queries,
    const std::shared_ptr<const std::vector<std::string>>& choices,
    std::vector<ValidationResult>& validations)
{
    if (value.algorithm == "ratio") register_batch(value, queries, choices, [](const std::string& first, const std::string& second) { return rapidfuzz::fuzz::ratio(first, second); }, validations);
    else if (value.algorithm == "partial_ratio") register_batch(value, queries, choices, [](const std::string& first, const std::string& second) { return rapidfuzz::fuzz::partial_ratio(first, second); }, validations);
    else if (value.algorithm == "token_sort_ratio") register_batch(value, queries, choices, [](const std::string& first, const std::string& second) { return rapidfuzz::fuzz::token_sort_ratio(first, second); }, validations);
    else if (value.algorithm == "token_set_ratio") register_batch(value, queries, choices, [](const std::string& first, const std::string& second) { return rapidfuzz::fuzz::token_set_ratio(first, second); }, validations);
    else if (value.algorithm == "partial_token_sort_ratio") register_batch(value, queries, choices, [](const std::string& first, const std::string& second) { return rapidfuzz::fuzz::partial_token_sort_ratio(first, second); }, validations);
    else if (value.algorithm == "partial_token_set_ratio") register_batch(value, queries, choices, [](const std::string& first, const std::string& second) { return rapidfuzz::fuzz::partial_token_set_ratio(first, second); }, validations);
    else if (value.algorithm == "qratio") register_batch(value, queries, choices, [](const std::string& first, const std::string& second) { return rapidfuzz::fuzz::QRatio(first, second); }, validations);
    else if (value.algorithm == "wratio") register_batch(value, queries, choices, [](const std::string& first, const std::string& second) { return rapidfuzz::fuzz::WRatio(first, second); }, validations);
    else throw std::runtime_error("unknown batch benchmark algorithm");
}

void write_validations(
    const std::string& path,
    const std::string& runtime,
    const std::string& corpus_sha,
    const std::vector<ValidationResult>& validations)
{
    std::ofstream stream(path);
    if (!stream) throw std::runtime_error("unable to write validation output");
    stream << "{\n  \"runtime\": \"" << json_escape(runtime) << "\",\n";
    stream << "  \"corpus_sha256\": \"" << json_escape(corpus_sha) << "\",\n  \"results\": [\n";
    stream << std::setprecision(17);
    for (std::size_t index = 0; index < validations.size(); ++index) {
        const ValidationResult& result = validations[index];
        stream << "    {\"case_id\": \"" << json_escape(result.case_id) << "\", ";
        if (result.batch) stream << "\"value\": null, \"digest\": \"" << result.digest << "\", ";
        else stream << "\"value\": " << result.value << ", \"digest\": null, ";
        stream << "\"count\": " << result.count << ", \"samples\": [";
        for (std::size_t sample_index = 0; sample_index < result.samples.size(); ++sample_index) {
            if (sample_index != 0) stream << ", ";
            stream << result.samples[sample_index];
        }
        stream << "]}" << (index + 1 == validations.size() ? "\n" : ",\n");
    }
    stream << "  ]\n}\n";
}

}

int main(int argc, char** argv)
{
    try {
        std::string directory = require_environment("RAPIDFUZZ_BENCHMARK_CORPUS");
        std::string tier = require_environment("RAPIDFUZZ_BENCHMARK_TIER");
        std::vector<BenchmarkCase> cases = read_cases(directory, tier);
        auto queries = std::make_shared<const std::vector<std::string>>(read_lines(directory + "/queries.txt"));
        auto choices = std::make_shared<const std::vector<std::string>>(read_lines(directory + "/choices.txt"));
        std::vector<ValidationResult> validations;
        for (const BenchmarkCase& benchmark_case : cases) {
            if (benchmark_case.category == "core") register_core(benchmark_case, validations);
            else if (benchmark_case.category == "fuzz") register_fuzz(benchmark_case, validations);
            else if (benchmark_case.category == "batch") register_batch_case(benchmark_case, queries, choices, validations);
            else throw std::runtime_error("unknown benchmark category");
        }
        const char* validation_path = std::getenv("RAPIDFUZZ_BENCHMARK_VALIDATION_OUTPUT");
        if (validation_path != nullptr && *validation_path != '\0') {
            std::string runtime = require_environment("RAPIDFUZZ_BENCHMARK_RUNTIME");
            std::string corpus_sha = require_environment("RAPIDFUZZ_BENCHMARK_CORPUS_SHA256");
            write_validations(validation_path, runtime, corpus_sha, validations);
        }
        benchmark::Initialize(&argc, argv);
        if (benchmark::ReportUnrecognizedArguments(argc, argv)) return 1;
        benchmark::RunSpecifiedBenchmarks();
        benchmark::Shutdown();
        return 0;
    }
    catch (const std::exception& error) {
        std::cerr << "Cross-language benchmark failed: " << error.what() << '\n';
        return 2;
    }
}
