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
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace PerformanceComparison;

/// <summary>
/// Collects and computes statistics for benchmark results.
/// </summary>
public sealed class BenchmarkStatistics
{
    private readonly List<double> _latencySamples = [];
    private long _messageCount;
    private readonly Stopwatch _stopwatch = new();

    /// <summary>
    /// Gets or sets the name of this benchmark run.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payload size used in this benchmark.
    /// </summary>
    public PayloadSize PayloadSize { get; set; }

    /// <summary>
    /// Starts the benchmark timer.
    /// </summary>
    public void Start() => _stopwatch.Start();

    /// <summary>
    /// Stops the benchmark timer.
    /// </summary>
    public void Stop() => _stopwatch.Stop();

    /// <summary>
    /// Records a message for throughput calculation.
    /// </summary>
    public void RecordMessage() => Interlocked.Increment(ref _messageCount);

    /// <summary>
    /// Records a latency sample in microseconds.
    /// </summary>
    public void RecordLatency(double latencyMicroseconds)
    {
        lock (_latencySamples)
        {
            _latencySamples.Add(latencyMicroseconds);
        }
    }

    /// <summary>
    /// Gets the total elapsed time.
    /// </summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>
    /// Gets the total message count.
    /// </summary>
    public long MessageCount => Interlocked.Read(ref _messageCount);

    /// <summary>
    /// Gets the throughput in messages per second.
    /// </summary>
    public double MessagesPerSecond => MessageCount / Elapsed.TotalSeconds;

    /// <summary>
    /// Gets the throughput in megabytes per second.
    /// </summary>
    public double MegabytesPerSecond
    {
        get
        {
            var payloadBytes = PayloadSize switch
            {
                PayloadSize.Small => 8,
                PayloadSize.Medium => 1024,
                PayloadSize.Large => 65536,
                _ => 8
            };
            return (MessageCount * payloadBytes) / (Elapsed.TotalSeconds * 1024 * 1024);
        }
    }

    /// <summary>
    /// Gets the minimum latency in microseconds.
    /// </summary>
    public double MinLatencyMicroseconds
    {
        get
        {
            lock (_latencySamples)
            {
                return _latencySamples.Count > 0 ? _latencySamples.Min() : 0;
            }
        }
    }

    /// <summary>
    /// Gets the maximum latency in microseconds.
    /// </summary>
    public double MaxLatencyMicroseconds
    {
        get
        {
            lock (_latencySamples)
            {
                return _latencySamples.Count > 0 ? _latencySamples.Max() : 0;
            }
        }
    }

    /// <summary>
    /// Gets the average latency in microseconds.
    /// </summary>
    public double AvgLatencyMicroseconds
    {
        get
        {
            lock (_latencySamples)
            {
                return _latencySamples.Count > 0 ? _latencySamples.Average() : 0;
            }
        }
    }

    /// <summary>
    /// Gets the P50 (median) latency in microseconds.
    /// </summary>
    public double P50LatencyMicroseconds => GetPercentile(50);

    /// <summary>
    /// Gets the P95 latency in microseconds.
    /// </summary>
    public double P95LatencyMicroseconds => GetPercentile(95);

    /// <summary>
    /// Gets the P99 latency in microseconds.
    /// </summary>
    public double P99LatencyMicroseconds => GetPercentile(99);

    /// <summary>
    /// Gets a specific percentile latency in microseconds.
    /// </summary>
    public double GetPercentile(double percentile)
    {
        lock (_latencySamples)
        {
            if (_latencySamples.Count == 0)
                return 0;

            var sorted = _latencySamples.OrderBy(x => x).ToList();
            var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
            index = Math.Max(0, Math.Min(index, sorted.Count - 1));
            return sorted[index];
        }
    }

    /// <summary>
    /// Gets the standard deviation of latency in microseconds.
    /// </summary>
    public double StdDevLatencyMicroseconds
    {
        get
        {
            lock (_latencySamples)
            {
                if (_latencySamples.Count < 2)
                    return 0;

                var avg = _latencySamples.Average();
                var sumOfSquares = _latencySamples.Sum(x => Math.Pow(x - avg, 2));
                return Math.Sqrt(sumOfSquares / (_latencySamples.Count - 1));
            }
        }
    }

    /// <summary>
    /// Gets the latency sample count.
    /// </summary>
    public int LatencySampleCount
    {
        get
        {
            lock (_latencySamples)
            {
                return _latencySamples.Count;
            }
        }
    }

    /// <summary>
    /// Resets all statistics.
    /// </summary>
    public void Reset()
    {
        _stopwatch.Reset();
        _messageCount = 0;
        lock (_latencySamples)
        {
            _latencySamples.Clear();
        }
    }
}

/// <summary>
/// Formats and prints benchmark results.
/// </summary>
public static class BenchmarkReporter
{
    /// <summary>
    /// Prints throughput results for a single benchmark.
    /// </summary>
    public static void PrintThroughputResults(BenchmarkStatistics stats)
    {
        Console.WriteLine();
        Console.WriteLine($"  {stats.Name}");
        Console.WriteLine($"  {"".PadRight(50, '-')}");
        Console.WriteLine($"  Messages sent:     {stats.MessageCount:N0}");
        Console.WriteLine($"  Duration:          {stats.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"  Throughput:        {stats.MessagesPerSecond:N0} msg/sec");
        Console.WriteLine($"  Data rate:         {stats.MegabytesPerSecond:F2} MB/sec");
    }

    /// <summary>
    /// Prints latency results for a single benchmark.
    /// </summary>
    public static void PrintLatencyResults(BenchmarkStatistics stats)
    {
        Console.WriteLine();
        Console.WriteLine($"  {stats.Name}");
        Console.WriteLine($"  {"".PadRight(50, '-')}");
        Console.WriteLine($"  Samples:           {stats.LatencySampleCount:N0}");
        Console.WriteLine($"  Min latency:       {stats.MinLatencyMicroseconds:F2} us");
        Console.WriteLine($"  Max latency:       {stats.MaxLatencyMicroseconds:F2} us");
        Console.WriteLine($"  Avg latency:       {stats.AvgLatencyMicroseconds:F2} us");
        Console.WriteLine($"  Std dev:           {stats.StdDevLatencyMicroseconds:F2} us");
        Console.WriteLine($"  P50 latency:       {stats.P50LatencyMicroseconds:F2} us");
        Console.WriteLine($"  P95 latency:       {stats.P95LatencyMicroseconds:F2} us");
        Console.WriteLine($"  P99 latency:       {stats.P99LatencyMicroseconds:F2} us");
    }

    /// <summary>
    /// Prints a comparison between two benchmark results.
    /// </summary>
    public static void PrintComparison(BenchmarkStatistics baseline, BenchmarkStatistics comparison)
    {
        Console.WriteLine();
        Console.WriteLine("  COMPARISON SUMMARY");
        Console.WriteLine($"  {"".PadRight(50, '=')}");
        Console.WriteLine($"  Baseline:          {baseline.Name}");
        Console.WriteLine($"  Comparison:        {comparison.Name}");
        Console.WriteLine();

        // Throughput comparison
        if (baseline.MessagesPerSecond > 0 && comparison.MessagesPerSecond > 0)
        {
            var throughputRatio = comparison.MessagesPerSecond / baseline.MessagesPerSecond;
            var indicator = throughputRatio > 1 ? "faster" : "slower";
            Console.WriteLine($"  Throughput ratio:  {throughputRatio:F2}x ({indicator})");
            Console.WriteLine($"    {baseline.Name}: {baseline.MessagesPerSecond:N0} msg/sec");
            Console.WriteLine($"    {comparison.Name}: {comparison.MessagesPerSecond:N0} msg/sec");
        }

        // Latency comparison (lower is better)
        if (baseline.LatencySampleCount > 0 && comparison.LatencySampleCount > 0)
        {
            var latencyRatio = baseline.AvgLatencyMicroseconds / comparison.AvgLatencyMicroseconds;
            var indicator = latencyRatio > 1 ? "lower latency" : "higher latency";
            Console.WriteLine();
            Console.WriteLine($"  Latency ratio:     {latencyRatio:F2}x ({indicator})");
            Console.WriteLine($"    {baseline.Name} avg: {baseline.AvgLatencyMicroseconds:F2} us");
            Console.WriteLine($"    {comparison.Name} avg: {comparison.AvgLatencyMicroseconds:F2} us");
        }
    }

    /// <summary>
    /// Prints a formatted header for benchmark output.
    /// </summary>
    public static void PrintHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"{"".PadRight(60, '=')}");
        Console.WriteLine($"  {title}");
        Console.WriteLine($"{"".PadRight(60, '=')}");
    }

    /// <summary>
    /// Prints benchmark configuration.
    /// </summary>
    public static void PrintConfig(BenchmarkConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("  BENCHMARK CONFIGURATION");
        Console.WriteLine($"  {"".PadRight(50, '-')}");
        Console.WriteLine($"  Mode:              {config.Mode}");
        Console.WriteLine($"  Payload size:      {config.PayloadSize} ({GetPayloadSizeBytes(config.PayloadSize)} bytes)");
        Console.WriteLine($"  Target:            {config.Target}");
        Console.WriteLine($"  Duration:          {config.DurationSeconds} seconds");
        Console.WriteLine($"  Warmup:            {config.WarmupSeconds} seconds");
        Console.WriteLine($"  Message count:     {config.MessageCount:N0}");
    }

    private static int GetPayloadSizeBytes(PayloadSize size) => size switch
    {
        PayloadSize.Small => 8,
        PayloadSize.Medium => 1024,
        PayloadSize.Large => 65536,
        _ => 8
    };
}