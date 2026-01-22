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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PerformanceComparison;

/// <summary>
/// Performance comparison benchmark between .NET System.Threading.Channels, System.IO.Pipes, and iceoryx2 C# IPC.
/// - Channels: In-process producer/consumer (not IPC)
/// - Pipes: Named pipes for true IPC
/// - iceoryx2: Zero-copy shared memory IPC
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
        var payloadSizes = new[] { PayloadSize.Small, PayloadSize.Medium, PayloadSize.Large, PayloadSize.ExtraLarge };
        var channelsResults = new List<BenchmarkStatistics>();
        var iceoryx2Results = new List<BenchmarkStatistics>();
        var pipesResults = new List<BenchmarkStatistics>();

        foreach (var payloadSize in payloadSizes)
        {
            config.PayloadSize = payloadSize;

            var sizeLabel = payloadSize switch
            {
                PayloadSize.Small => "Small (8 bytes)",
                PayloadSize.Medium => "Medium (1 KB)",
                PayloadSize.Large => "Large (64 KB)",
                PayloadSize.ExtraLarge => "Extra Large (512 KB)",
                _ => payloadSize.ToString()
            };

            BenchmarkReporter.PrintHeader($"{config.Mode} Benchmark - {sizeLabel}");

            var (channelStats, iox2Stats, pipesStats) = await RunSingleBenchmark(config);

            if (channelStats != null)
                channelsResults.Add(channelStats);
            if (iox2Stats != null)
                iceoryx2Results.Add(iox2Stats);
            if (pipesStats != null)
                pipesResults.Add(pipesStats);

            // Force GC between benchmarks
            ForceGarbageCollection();
        }

        // Print summary
        PrintSummary(config.Mode, config.Target, channelsResults, iceoryx2Results, pipesResults);

        // Generate report if requested
        if (config.GenerateReport)
        {
            GenerateAndSaveReport(config, channelsResults, pipesResults, iceoryx2Results);
        }
    }

    private static async Task RunBenchmark(BenchmarkConfig config)
    {
        var sizeLabel = config.PayloadSize switch
        {
            PayloadSize.Small => "Small (8 bytes)",
            PayloadSize.Medium => "Medium (1 KB)",
            PayloadSize.Large => "Large (64 KB)",
            PayloadSize.ExtraLarge => "Extra Large (512 KB)",
            _ => config.PayloadSize.ToString()
        };

        BenchmarkReporter.PrintHeader($"{config.Mode} Benchmark - {sizeLabel}");

        var (channelStats, iox2Stats, pipesStats) = await RunSingleBenchmark(config);

        // Print comparisons
        if (channelStats != null && iox2Stats != null)
        {
            BenchmarkReporter.PrintComparison(channelStats, iox2Stats);
        }
        if (pipesStats != null && iox2Stats != null)
        {
            BenchmarkReporter.PrintComparison(pipesStats, iox2Stats);
        }
        if (channelStats != null && pipesStats != null)
        {
            BenchmarkReporter.PrintComparison(channelStats, pipesStats);
        }

        // Generate report if requested
        if (config.GenerateReport)
        {
            var channelsResults = channelStats != null ? new List<BenchmarkStatistics> { channelStats } : new List<BenchmarkStatistics>();
            var pipesResults = pipesStats != null ? new List<BenchmarkStatistics> { pipesStats } : new List<BenchmarkStatistics>();
            var iceoryx2Results = iox2Stats != null ? new List<BenchmarkStatistics> { iox2Stats } : new List<BenchmarkStatistics>();

            GenerateAndSaveReport(config, channelsResults, pipesResults, iceoryx2Results);
        }
    }

    private static async Task<(BenchmarkStatistics? channels, BenchmarkStatistics? iceoryx2, BenchmarkStatistics? pipes)>
        RunSingleBenchmark(BenchmarkConfig config)
    {
        BenchmarkStatistics? channelStats = null;
        BenchmarkStatistics? iox2Stats = null;
        BenchmarkStatistics? pipesStats = null;

        // Run .NET Channels benchmark
        if (config.Target is BenchmarkTarget.Channels or BenchmarkTarget.Both or BenchmarkTarget.All)
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

        // Run Named Pipes benchmark
        if (config.Target is BenchmarkTarget.Pipes or BenchmarkTarget.All)
        {
            Console.WriteLine();
            Console.WriteLine("  Running Named Pipes benchmark...");
            var pipesBenchmark = new PipesBenchmark(config);

            try
            {
                pipesStats = config.Mode == BenchmarkMode.Throughput
                    ? await pipesBenchmark.RunThroughputAsync(config.PayloadSize)
                    : await pipesBenchmark.RunLatencyAsync(config.PayloadSize);

                if (config.Mode == BenchmarkMode.Throughput)
                    BenchmarkReporter.PrintThroughputResults(pipesStats);
                else
                    BenchmarkReporter.PrintLatencyResults(pipesStats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ERROR: Named Pipes benchmark failed: {ex.Message}");
            }

            // Force GC between benchmarks
            ForceGarbageCollection();
        }

        // Run iceoryx2 benchmark
        if (config.Target is BenchmarkTarget.Iceoryx2 or BenchmarkTarget.Both or BenchmarkTarget.All)
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

        return (channelStats, iox2Stats, pipesStats);
    }

    private static void PrintSummary(
        BenchmarkMode mode,
        BenchmarkTarget target,
        List<BenchmarkStatistics> channelsResults,
        List<BenchmarkStatistics> iceoryx2Results,
        List<BenchmarkStatistics> pipesResults)
    {
        BenchmarkReporter.PrintHeader("BENCHMARK SUMMARY");

        Console.WriteLine();
        Console.WriteLine("  Results by Payload Size:");

        var includeChannels = target is BenchmarkTarget.Channels or BenchmarkTarget.Both or BenchmarkTarget.All;
        var includePipes = target is BenchmarkTarget.Pipes or BenchmarkTarget.All;
        var includeIox2 = target is BenchmarkTarget.Iceoryx2 or BenchmarkTarget.Both or BenchmarkTarget.All;

        var maxResults = Math.Max(
            Math.Max(channelsResults.Count, iceoryx2Results.Count),
            pipesResults.Count);

        if (mode == BenchmarkMode.Throughput)
        {
            PrintThroughputSummary(includeChannels, includePipes, includeIox2, maxResults,
                channelsResults, pipesResults, iceoryx2Results);

            PrintEfficiencySummary(includeChannels, includePipes, includeIox2, maxResults,
                channelsResults, pipesResults, iceoryx2Results);
        }
        else
        {
            PrintLatencySummary(includeChannels, includePipes, includeIox2, maxResults,
                channelsResults, pipesResults, iceoryx2Results);
        }

        Console.WriteLine();
        Console.WriteLine("  Note: Ratio > 1.0 means iceoryx2 is faster (for throughput),");
        Console.WriteLine("        more CPU efficient (for efficiency), or has lower latency.");
        Console.WriteLine("        Channels is in-process only. Pipes and iceoryx2 are true IPC.");
    }

    private static void PrintThroughputSummary(
        bool includeChannels, bool includePipes, bool includeIox2, int maxResults,
        List<BenchmarkStatistics> channelsResults,
        List<BenchmarkStatistics> pipesResults,
        List<BenchmarkStatistics> iceoryx2Results)
    {
        // Build header based on what's included
        var header = $"  {"Payload",-12}";
        if (includeChannels) header += $" {"Channels",-14}";
        if (includePipes) header += $" {"Pipes",-14}";
        if (includeIox2) header += $" {"iceoryx2",-14}";
        if (includePipes && includeIox2) header += $" {"iox2/Pipes",-10}";

        var lineWidth = header.Length;
        Console.WriteLine($"  {"".PadRight(lineWidth - 2, '-')}");
        Console.WriteLine(header);
        Console.WriteLine($"  {"".PadRight(lineWidth - 2, '-')}");

        for (var i = 0; i < maxResults; i++)
        {
            var channelStats = i < channelsResults.Count ? channelsResults[i] : null;
            var pipesStats = i < pipesResults.Count ? pipesResults[i] : null;
            var iox2Stats = i < iceoryx2Results.Count ? iceoryx2Results[i] : null;

            var payloadSize = channelStats?.PayloadSize ?? pipesStats?.PayloadSize ?? iox2Stats?.PayloadSize ?? PayloadSize.Small;
            var payloadLabel = GetPayloadLabel(payloadSize);

            var line = $"  {payloadLabel,-12}";
            if (includeChannels) line += $" {FormatThroughput(channelStats?.MessagesPerSecond ?? 0),14}";
            if (includePipes) line += $" {FormatThroughput(pipesStats?.MessagesPerSecond ?? 0),14}";
            if (includeIox2) line += $" {FormatThroughput(iox2Stats?.MessagesPerSecond ?? 0),14}";

            if (includePipes && includeIox2)
            {
                var pipesT = pipesStats?.MessagesPerSecond ?? 0;
                var iox2T = iox2Stats?.MessagesPerSecond ?? 0;
                var ratio = pipesT > 0 && iox2T > 0 ? iox2T / pipesT : 0;
                line += $" {(ratio > 0 ? $"{ratio:F1}x" : "N/A"),10}";
            }

            Console.WriteLine(line);
        }
        Console.WriteLine($"  {"".PadRight(lineWidth - 2, '-')}");
    }

    private static void PrintEfficiencySummary(
        bool includeChannels, bool includePipes, bool includeIox2, int maxResults,
        List<BenchmarkStatistics> channelsResults,
        List<BenchmarkStatistics> pipesResults,
        List<BenchmarkStatistics> iceoryx2Results)
    {
        Console.WriteLine();
        Console.WriteLine("  CPU Efficiency (messages per CPU-second):");

        var header = $"  {"Payload",-12}";
        if (includeChannels) header += $" {"Channels",-14}";
        if (includePipes) header += $" {"Pipes",-14}";
        if (includeIox2) header += $" {"iceoryx2",-14}";
        if (includePipes && includeIox2) header += $" {"iox2/Pipes",-10}";

        var lineWidth = header.Length;
        Console.WriteLine($"  {"".PadRight(lineWidth - 2, '-')}");
        Console.WriteLine(header);
        Console.WriteLine($"  {"".PadRight(lineWidth - 2, '-')}");

        for (var i = 0; i < maxResults; i++)
        {
            var channelStats = i < channelsResults.Count ? channelsResults[i] : null;
            var pipesStats = i < pipesResults.Count ? pipesResults[i] : null;
            var iox2Stats = i < iceoryx2Results.Count ? iceoryx2Results[i] : null;

            var payloadSize = channelStats?.PayloadSize ?? pipesStats?.PayloadSize ?? iox2Stats?.PayloadSize ?? PayloadSize.Small;
            var payloadLabel = GetPayloadLabel(payloadSize);

            var line = $"  {payloadLabel,-12}";
            if (includeChannels) line += $" {FormatThroughput(channelStats?.MessagesPerCpuSecond ?? 0),14}";
            if (includePipes) line += $" {FormatThroughput(pipesStats?.MessagesPerCpuSecond ?? 0),14}";
            if (includeIox2) line += $" {FormatThroughput(iox2Stats?.MessagesPerCpuSecond ?? 0),14}";

            if (includePipes && includeIox2)
            {
                var pipesE = pipesStats?.MessagesPerCpuSecond ?? 0;
                var iox2E = iox2Stats?.MessagesPerCpuSecond ?? 0;
                var ratio = pipesE > 0 && iox2E > 0 ? iox2E / pipesE : 0;
                line += $" {(ratio > 0 ? $"{ratio:F1}x" : "N/A"),10}";
            }

            Console.WriteLine(line);
        }
        Console.WriteLine($"  {"".PadRight(lineWidth - 2, '-')}");
    }

    private static void PrintLatencySummary(
        bool includeChannels, bool includePipes, bool includeIox2, int maxResults,
        List<BenchmarkStatistics> channelsResults,
        List<BenchmarkStatistics> pipesResults,
        List<BenchmarkStatistics> iceoryx2Results)
    {
        var header = $"  {"Payload",-12}";
        if (includeChannels) header += $" {"Channels P50",-14}";
        if (includePipes) header += $" {"Pipes P50",-14}";
        if (includeIox2) header += $" {"iceoryx2 P50",-14}";
        if (includePipes && includeIox2) header += $" {"Pipes/iox2",-10}";

        var lineWidth = header.Length;
        Console.WriteLine($"  {"".PadRight(lineWidth - 2, '-')}");
        Console.WriteLine(header);
        Console.WriteLine($"  {"".PadRight(lineWidth - 2, '-')}");

        for (var i = 0; i < maxResults; i++)
        {
            var channelStats = i < channelsResults.Count ? channelsResults[i] : null;
            var pipesStats = i < pipesResults.Count ? pipesResults[i] : null;
            var iox2Stats = i < iceoryx2Results.Count ? iceoryx2Results[i] : null;

            var payloadSize = channelStats?.PayloadSize ?? pipesStats?.PayloadSize ?? iox2Stats?.PayloadSize ?? PayloadSize.Small;
            var payloadLabel = GetPayloadLabel(payloadSize);

            var line = $"  {payloadLabel,-12}";
            if (includeChannels) line += $" {FormatLatency(channelStats?.P50LatencyMicroseconds ?? 0),14}";
            if (includePipes) line += $" {FormatLatency(pipesStats?.P50LatencyMicroseconds ?? 0),14}";
            if (includeIox2) line += $" {FormatLatency(iox2Stats?.P50LatencyMicroseconds ?? 0),14}";

            if (includePipes && includeIox2)
            {
                var pipesL = pipesStats?.P50LatencyMicroseconds ?? 0;
                var iox2L = iox2Stats?.P50LatencyMicroseconds ?? 0;
                var ratio = pipesL > 0 && iox2L > 0 ? pipesL / iox2L : 0;
                line += $" {(ratio > 0 ? $"{ratio:F1}x" : "N/A"),10}";
            }

            Console.WriteLine(line);
        }
        Console.WriteLine($"  {"".PadRight(lineWidth - 2, '-')}");
    }

    private static string GetPayloadLabel(PayloadSize size) => size switch
    {
        PayloadSize.Small => "Small (8B)",
        PayloadSize.Medium => "Medium (1KB)",
        PayloadSize.Large => "Large (64KB)",
        PayloadSize.ExtraLarge => "XL (512KB)",
        _ => size.ToString()
    };

    private static string FormatThroughput(double value) =>
        value > 0 ? $"{value:N0}" : "N/A";

    private static string FormatLatency(double value) =>
        value > 0 ? $"{value:F2} us" : "N/A";

    private static void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static void GenerateAndSaveReport(
        BenchmarkConfig config,
        List<BenchmarkStatistics> channelsResults,
        List<BenchmarkStatistics> pipesResults,
        List<BenchmarkStatistics> iceoryx2Results)
    {
        Console.WriteLine();
        Console.WriteLine("  Generating benchmark report...");

        var generator = new ReportGenerator();
        var report = generator.Generate(config, channelsResults, pipesResults, iceoryx2Results);

        var reportPath = Path.GetFullPath(config.ReportPath);
        File.WriteAllText(reportPath, report);

        Console.WriteLine($"  Report saved to: {reportPath}");
    }
}