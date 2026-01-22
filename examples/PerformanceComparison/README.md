# iceoryx2 C# Performance Comparison Benchmark

A comprehensive benchmark comparing the performance of three communication mechanisms in .NET:

| Technology | Type | Zero-Copy | Kernel Bypass |
|------------|------|-----------|---------------|
| **System.Threading.Channels** | In-process only | No (managed heap) | N/A |
| **System.IO.Pipes (Named Pipes)** | True IPC | No | No |
| **iceoryx2** | True IPC | **Yes** | **Yes** |

## Overview

This benchmark measures throughput, latency, and CPU efficiency across different payload sizes to help you understand when to use each technology.

**Key Findings:**
- For true IPC workloads, iceoryx2 is **3.8x to 3,307x faster** than Named Pipes depending on payload size
- iceoryx2 uses **6x less CPU** than Named Pipes
- iceoryx2 maintains **constant throughput** regardless of payload size (zero-copy advantage)

## Prerequisites

- .NET 8.0 or .NET 9.0 SDK
- iceoryx2 native library installed and accessible

## Building

```bash
dotnet build -c Release
```

## Usage

```bash
dotnet run -f net8.0 -- [OPTIONS]
```

### Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--mode` | `-m` | Benchmark mode: `throughput` (t) or `latency` (l) | throughput |
| `--payload` | `-p` | Payload size: `small` (s, 8B), `medium` (m, 1KB), `large` (l, 64KB), `xl` (x, 512KB) | small |
| `--target` | `-t` | Target: `channels` (c), `iceoryx2` (i), `pipes` (p), `both` (b), `all` (a) | both |
| `--duration` | `-d` | Benchmark duration in seconds (for throughput) | 10 |
| `--warmup` | `-w` | Warmup duration in seconds | 2 |
| `--messages` | `-n` | Number of messages (for latency) | 100,000 |
| `--capacity` | `-c` | Channel capacity for bounded channels | 1024 |
| `--all-payloads` | `-a` | Run benchmarks for all payload sizes | false |
| `--report` | `-r` | Generate a markdown report after benchmarking | false |
| `--report-path` | | Path to save the report (implies --report) | BENCHMARK_REPORT.md |
| `--help` | `-h` | Show help message | |

### Target Options

- `channels` / `c` - System.Threading.Channels only (in-process, not IPC)
- `iceoryx2` / `i` / `iox2` - iceoryx2 only (true IPC)
- `pipes` / `p` - Named Pipes only (true IPC)
- `both` / `b` - Channels + iceoryx2 (default)
- `all` / `a` - All three technologies

## Examples

### Basic Throughput Benchmark

```bash
# Default: Channels vs iceoryx2, small payload, 10 seconds
dotnet run -f net8.0

# All technologies comparison
dotnet run -f net8.0 -- -t all
```

### All Payload Sizes

```bash
# Run all payload sizes (8B, 1KB, 64KB, 512KB)
dotnet run -f net8.0 -- -t all -a

# With report generation
dotnet run -f net8.0 -- -t all -a --report
```

### Latency Benchmark

```bash
# Latency test with 100,000 messages
dotnet run -f net8.0 -- -m latency -t all

# Latency test with custom message count
dotnet run -f net8.0 -- -m latency -n 50000 -t iceoryx2
```

### Specific Configurations

```bash
# iceoryx2 only, large payload
dotnet run -f net8.0 -- -t iceoryx2 -p large

# Named Pipes vs iceoryx2, medium payload, 30 second duration
dotnet run -f net8.0 -- -t all -p medium -d 30

# Quick test with short duration
dotnet run -f net8.0 -- -t all -d 5 -w 1
```

### Generate Report

```bash
# Generate default report (BENCHMARK_REPORT.md)
dotnet run -f net8.0 -- -t all -a --report

# Generate report with custom path
dotnet run -f net8.0 -- -t all -a --report-path results/benchmark_$(date +%Y%m%d).md
```

## Metrics Measured

### Throughput Mode
- **Messages/second** - Raw message throughput
- **Data rate (MB/s)** - Bytes transferred per second
- **CPU utilization (%)** - CPU time relative to wall time (>100% = multi-core)
- **CPU efficiency (msg/CPU-sec)** - Messages per CPU-second consumed

### Latency Mode
- **Min latency** - Minimum observed latency
- **P50 latency** - Median latency (50th percentile)
- **P95 latency** - 95th percentile latency
- **P99 latency** - 99th percentile latency
- **Max latency** - Maximum observed latency

## Understanding the Results

### Why Channels Shows High Throughput

System.Threading.Channels shows very high throughput because:
- It operates **in-process only** (not IPC)
- It passes object **references**, not data copies
- It cannot communicate between processes

Use Channels when you need fast in-process producer/consumer patterns.

### Why Named Pipes Degrades with Large Payloads

Named Pipes throughput degrades dramatically with larger payloads because:
- Every byte must be **copied twice** (user space -> kernel -> user space)
- Multiple **kernel context switches** per message
- High CPU overhead from async I/O machinery

### Why iceoryx2 Maintains Constant Throughput

iceoryx2 maintains constant throughput regardless of payload size because:
- **Zero-copy**: Data is written directly to shared memory
- **Kernel bypass**: No kernel involvement in data transfer
- Only pointers/references are exchanged between processes

## Payload Sizes

| Name | Size | Use Case |
|------|------|----------|
| Small | 8 bytes | Timestamps, counters, signals |
| Medium | 1 KB | Sensor data, small messages |
| Large | 64 KB | Images, audio frames |
| Extra Large | 512 KB | Video frames, large buffers |

## When to Use Each Technology

| Use Case | Recommendation |
|----------|----------------|
| In-process producer/consumer | **Channels** - simplest, fastest for single process |
| Cross-process, simple setup | Named Pipes - widely supported, easy to use |
| Cross-process, high throughput | **iceoryx2** - zero-copy, kernel bypass |
| Cross-process, large payloads | **iceoryx2** - maintains performance at any size |
| Real-time / low-latency | **iceoryx2** - sub-microsecond P50 latency |
| Battery/power constrained | **iceoryx2** - 6x less CPU than Pipes |

## Project Structure

```
PerformanceComparison/
├── Program.cs              # Main entry point and orchestration
├── BenchmarkConfig.cs      # CLI parsing and configuration
├── BenchmarkStatistics.cs  # Statistics collection and calculation
├── ChannelsBenchmark.cs    # System.Threading.Channels benchmark
├── PipesBenchmark.cs       # System.IO.Pipes benchmark
├── Iceoryx2Benchmark.cs    # iceoryx2 benchmark
├── PayloadTypes.cs         # Payload structs and enums
├── ReportGenerator.cs      # Markdown report generation
└── README.md               # This file
```

## License

This project is licensed under the Apache License 2.0 or MIT License - see the license headers in the source files for details.
