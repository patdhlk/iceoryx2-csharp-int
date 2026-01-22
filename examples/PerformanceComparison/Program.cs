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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PerformanceComparison;

/// <summary>
/// Performance comparison benchmark between .NET System.Threading.Channels and iceoryx2 C# IPC.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("============================================================");
        Console.WriteLine("  iceoryx2 C# Performance Comparison Benchmark");
        Console.WriteLine("============================================================");
        Console.WriteLine();

        // Print system information
        PrintSystemInfo();

        // Parse configuration
        var config = BenchmarkConfig.Parse(args);
        BenchmarkReporter.PrintConfig(config);

        // Force GC before starting
        ForceGarbageCollection();

        // Run benchmarks
        if (config.RunAllPayloadSizes)
        {
            await RunAllPayloadSizes(config);
        }
        else
        {
            await RunBenchmark(config);
        }

        Console.WriteLine();
        Console.WriteLine("============================================================");
        Console.WriteLine("  Benchmark Complete");
        Console.WriteLine("============================================================");
        Console.WriteLine();
    }

    private static void PrintSystemInfo()
    {
        Console.WriteLine("  SYSTEM INFORMATION");
        Console.WriteLine($"  {"".PadRight(50, '-')}");
        Console.WriteLine($"  OS:                {RuntimeInformation.OSDescription}");
        Console.WriteLine($"  Architecture:      {RuntimeInformation.OSArchitecture}");
        Console.WriteLine($"  .NET Version:      {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"  Processors:        {Environment.ProcessorCount}");
    }

    private static async Task RunAllPayloadSizes(BenchmarkConfig config)
    {
        var payloadSizes = new[] { PayloadSize.Small, PayloadSize.Medium, PayloadSize.Large };
        var channelsResults = new List<BenchmarkStatistics>();
        var iceoryx2Results = new List<BenchmarkStatistics>();

        foreach (var payloadSize in payloadSizes)
        {
            config.PayloadSize = payloadSize;

            var sizeLabel = payloadSize switch
            {
                PayloadSize.Small => "Small (8 bytes)",
                PayloadSize.Medium => "Medium (1 KB)",
                PayloadSize.Large => "Large (64 KB)",
                _ => payloadSize.ToString()
            };

            BenchmarkReporter.PrintHeader($"{config.Mode} Benchmark - {sizeLabel}");

            var (channelStats, iox2Stats) = await RunSingleBenchmark(config);

            if (channelStats != null)
                channelsResults.Add(channelStats);
            if (iox2Stats != null)
                iceoryx2Results.Add(iox2Stats);

            // Force GC between benchmarks
            ForceGarbageCollection();
        }

        // Print summary
        PrintSummary(config.Mode, channelsResults, iceoryx2Results);
    }

    private static async Task RunBenchmark(BenchmarkConfig config)
    {
        var sizeLabel = config.PayloadSize switch
        {
            PayloadSize.Small => "Small (8 bytes)",
            PayloadSize.Medium => "Medium (1 KB)",
            PayloadSize.Large => "Large (64 KB)",
            _ => config.PayloadSize.ToString()
        };

        BenchmarkReporter.PrintHeader($"{config.Mode} Benchmark - {sizeLabel}");

        var (channelStats, iox2Stats) = await RunSingleBenchmark(config);

        // Print comparison if both were run
        if (channelStats != null && iox2Stats != null)
        {
            BenchmarkReporter.PrintComparison(channelStats, iox2Stats);
        }
    }

    private static async Task<(BenchmarkStatistics? channels, BenchmarkStatistics? iceoryx2)>
        RunSingleBenchmark(BenchmarkConfig config)
    {
        BenchmarkStatistics? channelStats = null;
        BenchmarkStatistics? iox2Stats = null;

        // Run .NET Channels benchmark
        if (config.Target is BenchmarkTarget.Channels or BenchmarkTarget.Both)
        {
            Console.WriteLine();
            Console.WriteLine("  Running .NET Channels benchmark...");
            var channelsBenchmark = new ChannelsBenchmark(config);

            channelStats = config.Mode == BenchmarkMode.Throughput
                ? await channelsBenchmark.RunThroughputAsync(config.PayloadSize)
                : await channelsBenchmark.RunLatencyAsync(config.PayloadSize);

            if (config.Mode == BenchmarkMode.Throughput)
                BenchmarkReporter.PrintThroughputResults(channelStats);
            else
                BenchmarkReporter.PrintLatencyResults(channelStats);

            // Force GC between benchmarks
            ForceGarbageCollection();
        }

        // Run iceoryx2 benchmark
        if (config.Target is BenchmarkTarget.Iceoryx2 or BenchmarkTarget.Both)
        {
            Console.WriteLine();
            Console.WriteLine("  Running iceoryx2 benchmark...");
            var iox2Benchmark = new Iceoryx2Benchmark(config);

            try
            {
                iox2Stats = config.Mode == BenchmarkMode.Throughput
                    ? await iox2Benchmark.RunThroughputAsync(config.PayloadSize)
                    : await iox2Benchmark.RunLatencyAsync(config.PayloadSize);

                if (config.Mode == BenchmarkMode.Throughput)
                    BenchmarkReporter.PrintThroughputResults(iox2Stats);
                else
                    BenchmarkReporter.PrintLatencyResults(iox2Stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ERROR: iceoryx2 benchmark failed: {ex.Message}");
                Console.WriteLine("    Make sure the iceoryx2 native library is available.");
            }
        }

        return (channelStats, iox2Stats);
    }

    private static void PrintSummary(
        BenchmarkMode mode,
        List<BenchmarkStatistics> channelsResults,
        List<BenchmarkStatistics> iceoryx2Results)
    {
        BenchmarkReporter.PrintHeader("BENCHMARK SUMMARY");

        Console.WriteLine();
        Console.WriteLine("  Results by Payload Size:");
        Console.WriteLine($"  {"".PadRight(70, '-')}");

        if (mode == BenchmarkMode.Throughput)
        {
            Console.WriteLine($"  {"Payload",-12} {"Channels (msg/s)",-20} {"iceoryx2 (msg/s)",-20} {"Ratio",-10}");
            Console.WriteLine($"  {"".PadRight(70, '-')}");

            for (var i = 0; i < Math.Max(channelsResults.Count, iceoryx2Results.Count); i++)
            {
                var channelStats = i < channelsResults.Count ? channelsResults[i] : null;
                var iox2Stats = i < iceoryx2Results.Count ? iceoryx2Results[i] : null;

                var payloadSize = channelStats?.PayloadSize ?? iox2Stats?.PayloadSize ?? PayloadSize.Small;
                var payloadLabel = payloadSize switch
                {
                    PayloadSize.Small => "Small (8B)",
                    PayloadSize.Medium => "Medium (1KB)",
                    PayloadSize.Large => "Large (64KB)",
                    _ => payloadSize.ToString()
                };

                var channelThroughput = channelStats?.MessagesPerSecond ?? 0;
                var iox2Throughput = iox2Stats?.MessagesPerSecond ?? 0;
                var ratio = channelThroughput > 0 && iox2Throughput > 0
                    ? iox2Throughput / channelThroughput
                    : 0;

                var ratioStr = ratio > 0 ? $"{ratio:F2}x" : "N/A";

                Console.WriteLine($"  {payloadLabel,-12} {channelThroughput,18:N0} {iox2Throughput,18:N0} {ratioStr,10}");
            }
        }
        else
        {
            Console.WriteLine($"  {"Payload",-12} {"Channels P50 (us)",-18} {"iceoryx2 P50 (us)",-18} {"Ratio",-10}");
            Console.WriteLine($"  {"".PadRight(70, '-')}");

            for (var i = 0; i < Math.Max(channelsResults.Count, iceoryx2Results.Count); i++)
            {
                var channelStats = i < channelsResults.Count ? channelsResults[i] : null;
                var iox2Stats = i < iceoryx2Results.Count ? iceoryx2Results[i] : null;

                var payloadSize = channelStats?.PayloadSize ?? iox2Stats?.PayloadSize ?? PayloadSize.Small;
                var payloadLabel = payloadSize switch
                {
                    PayloadSize.Small => "Small (8B)",
                    PayloadSize.Medium => "Medium (1KB)",
                    PayloadSize.Large => "Large (64KB)",
                    _ => payloadSize.ToString()
                };

                var channelLatency = channelStats?.P50LatencyMicroseconds ?? 0;
                var iox2Latency = iox2Stats?.P50LatencyMicroseconds ?? 0;
                var ratio = channelLatency > 0 && iox2Latency > 0
                    ? channelLatency / iox2Latency
                    : 0;

                var ratioStr = ratio > 0 ? $"{ratio:F2}x" : "N/A";

                Console.WriteLine($"  {payloadLabel,-12} {channelLatency,16:F2} {iox2Latency,16:F2} {ratioStr,10}");
            }
        }

        Console.WriteLine($"  {"".PadRight(70, '-')}");
        Console.WriteLine();
        Console.WriteLine("  Note: Ratio > 1.0 means iceoryx2 is faster (for throughput) or");
        Console.WriteLine("        has lower latency (for latency tests).");
    }

    private static void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}