cmake_minimum_required(VERSION 3.22)

project(rapidfuzz_cross_language_benchmarks LANGUAGES CXX)

if(NOT DEFINED RAPIDFUZZ_CPP_SOURCE_DIR)
    message(FATAL_ERROR "RAPIDFUZZ_CPP_SOURCE_DIR is required")
endif()

if(NOT DEFINED GOOGLE_BENCHMARK_SOURCE_DIR)
    message(FATAL_ERROR "GOOGLE_BENCHMARK_SOURCE_DIR is required")
endif()

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(BENCHMARK_ENABLE_TESTING OFF CACHE BOOL "" FORCE)
set(BENCHMARK_ENABLE_GTEST_TESTS OFF CACHE BOOL "" FORCE)
set(BENCHMARK_ENABLE_INSTALL OFF CACHE BOOL "" FORCE)

add_subdirectory("${GOOGLE_BENCHMARK_SOURCE_DIR}" "${CMAKE_BINARY_DIR}/google-benchmark" EXCLUDE_FROM_ALL)

add_executable(rapidfuzz_common_benchmarks common_benchmarks.cpp)
target_include_directories(rapidfuzz_common_benchmarks PRIVATE "${RAPIDFUZZ_CPP_SOURCE_DIR}")
target_link_libraries(rapidfuzz_common_benchmarks PRIVATE benchmark::benchmark)
target_compile_definitions(rapidfuzz_common_benchmarks PRIVATE $<$<CONFIG:Release>:NDEBUG>)

if(CMAKE_CXX_COMPILER_ID MATCHES "GNU|Clang")
    target_compile_options(rapidfuzz_common_benchmarks PRIVATE $<$<CONFIG:Release>:-O3;-march=native>)
endif()
