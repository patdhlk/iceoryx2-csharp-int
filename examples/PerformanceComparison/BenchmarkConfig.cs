// Copyright (c) 2025 Contributors to the Eclipse Foundation
//
// See the NOTICE file(s) distributed with this work for additional
// information regarding copyright ownership.
//
// This program and the accompanying materials are made available under the
// terms of the Apache Software License 2.0 which is available at
// https://www.apache.org/licenses/LICENSE-2.0, or the MIT license
// which is available at https://opensource.org/licenses/MIT.
//
// SPDX-License-Identifier: Apache-2.0 OR MIT

using System;

namespace PerformanceComparison;

/// <summary>
/// Configuration for benchmark runs.
/// </summary>
public sealed class BenchmarkConfig
{
    /// <summary>
    /// The benchmark mode (throughput or latency).
    /// </summary>
    public BenchmarkMode Mode { get; set; } = BenchmarkMode.Throughput;

    /// <summary>
    /// The payload size to use.
    /// </summary>
    public PayloadSize PayloadSize { get; set; } = PayloadSize.Small;

    /// <summary>
    /// The target system(s) to benchmark.
    /// </summary>
    public BenchmarkTarget Target { get; set; } = BenchmarkTarget.Both;

    /// <summary>
    /// Duration of the benchmark in seconds (for throughput tests).
    /// </summary>
    public int DurationSeconds { get; set; } = 10;

    /// <summary>
    /// Warmup duration in seconds.
    /// </summary>
    public int WarmupSeconds { get; set; } = 2;

    /// <summary>
    /// Number of messages to send (for latency tests).
    /// </summary>
    public int MessageCount { get; set; } = 100_000;

    /// <summary>
    /// Channel capacity for bounded channels.
    /// </summary>
    public int ChannelCapacity { get; set; } = 1024;

    /// <summary>
    /// Whether to run all payload sizes.
    /// </summary>
    public bool RunAllPayloadSizes { get; set; }

    /// <summary>
    /// Whether to generate a markdown report after benchmarking.
    /// </summary>
    public bool GenerateReport { get; set; }

    /// <summary>
    /// Path to save the generated report. Defaults to BENCHMARK_REPORT.md in the current directory.
    /// </summary>
    public string ReportPath { get; set; } = "BENCHMARK_REPORT.md";

    /// <summary>
    /// Parses command line arguments into a BenchmarkConfig.
    /// </summary>
    public static BenchmarkConfig Parse(string[] args)
    {
        var config = new BenchmarkConfig();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();

            switch (arg)
            {
                case "--mode" or "-m":
                    if (i + 1 < args.Length)
                    {
                        config.Mode = args[++i].ToLowerInvariant() switch
                        {
                            "throughput" or "t" => BenchmarkMode.Throughput,
                            "latency" or "l" => BenchmarkMode.Latency,
                            _ => BenchmarkMode.Throughput
                        };
                    }
                    break;

                case "--payload" or "-p":
                    if (i + 1 < args.Length)
                    {
                        config.PayloadSize = args[++i].ToLowerInvariant() switch
                        {
                            "small" or "s" => PayloadSize.Small,
                            "medium" or "m" => PayloadSize.Medium,
                            "large" or "l" => PayloadSize.Large,
                            _ => PayloadSize.Small
                        };
                    }
                    break;

                case "--target" or "-t":
                    if (i + 1 < args.Length)
                    {
                        config.Target = args[++i].ToLowerInvariant() switch
                        {
                            "channels" or "c" => BenchmarkTarget.Channels,
                            "iceoryx2" or "i" or "iox2" => BenchmarkTarget.Iceoryx2,
                            "pipes" or "p" => BenchmarkTarget.Pipes,
                            "both" or "b" => BenchmarkTarget.Both,
                            "all" or "a" => BenchmarkTarget.All,
                            _ => BenchmarkTarget.Both
                        };
                    }
                    break;

                case "--duration" or "-d":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var duration))
                    {
                        config.DurationSeconds = duration;
                    }
                    break;

                case "--warmup" or "-w":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var warmup))
                    {
                        config.WarmupSeconds = warmup;
                    }
                    break;

                case "--messages" or "-n":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var messages))
                    {
                        config.MessageCount = messages;
                    }
                    break;

                case "--capacity" or "-c":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var capacity))
                    {
                        config.ChannelCapacity = capacity;
                    }
                    break;

                case "--all-payloads" or "-a":
                    config.RunAllPayloadSizes = true;
                    break;

                case "--report" or "-r":
                    config.GenerateReport = true;
                    break;

                case "--report-path":
                    if (i + 1 < args.Length)
                    {
                        config.ReportPath = args[++i];
                        config.GenerateReport = true;
                    }
                    break;

                case "--help" or "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return config;
    }

    /// <summary>
    /// Prints help information.
    /// </summary>
    public static void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("iceoryx2 C# Performance Comparison Benchmark");
        Console.WriteLine("=============================================");
        Console.WriteLine();
        Console.WriteLine("Compares performance between .NET System.Threading.Channels, System.IO.Pipes, and iceoryx2 IPC.");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  dotnet run -- [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  -m, --mode <MODE>        Benchmark mode: throughput (t) or latency (l)");
        Console.WriteLine("                           Default: throughput");
        Console.WriteLine();
        Console.WriteLine("  -p, --payload <SIZE>     Payload size: small (s, 8B), medium (m, 1KB), large (l, 64KB)");
        Console.WriteLine("                           Default: small");
        Console.WriteLine();
        Console.WriteLine("  -t, --target <TARGET>    Target: channels (c), iceoryx2 (i), pipes (p),");
        Console.WriteLine("                           both (b), or all (a). 'both' = channels+iceoryx2.");
        Console.WriteLine("                           Default: both");
        Console.WriteLine();
        Console.WriteLine("  -d, --duration <SECS>    Benchmark duration in seconds (for throughput)");
        Console.WriteLine("                           Default: 10");
        Console.WriteLine();
        Console.WriteLine("  -w, --warmup <SECS>      Warmup duration in seconds");
        Console.WriteLine("                           Default: 2");
        Console.WriteLine();
        Console.WriteLine("  -n, --messages <COUNT>   Number of messages (for latency)");
        Console.WriteLine("                           Default: 100000");
        Console.WriteLine();
        Console.WriteLine("  -c, --capacity <SIZE>    Channel capacity for bounded channels");
        Console.WriteLine("                           Default: 1024");
        Console.WriteLine();
        Console.WriteLine("  -a, --all-payloads       Run benchmarks for all payload sizes");
        Console.WriteLine();
        Console.WriteLine("  -r, --report             Generate a markdown report after benchmarking");
        Console.WriteLine();
        Console.WriteLine("  --report-path <PATH>     Path to save the report (implies --report)");
        Console.WriteLine("                           Default: BENCHMARK_REPORT.md");
        Console.WriteLine();
        Console.WriteLine("  -h, --help               Show this help message");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  dotnet run -- --mode throughput --payload small --target both");
        Console.WriteLine("  dotnet run -- -m latency -p medium -t iceoryx2");
        Console.WriteLine("  dotnet run -- --all-payloads --mode throughput");
        Console.WriteLine("  dotnet run -- -t all -a                         # All targets, all payloads");
        Console.WriteLine("  dotnet run -- -t pipes -p large                 # Named pipes with 64KB payload");
        Console.WriteLine();
    }
}