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
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PerformanceComparison;

/// <summary>
/// Benchmark implementation using .NET System.Threading.Channels.
/// </summary>
public sealed class ChannelsBenchmark
{
    private readonly BenchmarkConfig _config;

    public ChannelsBenchmark(BenchmarkConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Runs the throughput benchmark for the specified payload size.
    /// </summary>
    public async Task<BenchmarkStatistics> RunThroughputAsync(PayloadSize payloadSize)
    {
        // Use byte arrays for channels since large structs cause TypeLoadException
        var payloadBytes = payloadSize switch
        {
            PayloadSize.Small => 8,
            PayloadSize.Medium => 1024,
            PayloadSize.Large => 65536,
            PayloadSize.ExtraLarge => 524288,
            _ => 8
        };

        return await RunThroughputWithByteArrayAsync(payloadSize, payloadBytes);
    }

    private async Task<BenchmarkStatistics> RunThroughputWithByteArrayAsync(PayloadSize payloadSize, int payloadBytes)
    {
        var stats = new BenchmarkStatistics
        {
            Name = $".NET Channels ({payloadSize})",
            PayloadSize = payloadSize
        };

        // Use DropOldest to match iceoryx2's EnableSafeOverflow(true) behavior
        var options = new BoundedChannelOptions(_config.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true
        };
        var channel = Channel.CreateBounded<byte[]>(options);

        using var cts = new CancellationTokenSource();
        var warmupComplete = false;
        var benchmarkComplete = false;

        // Pre-allocate a reusable payload
        var payload = new byte[payloadBytes];

        // Producer task - uses synchronous TryWrite to match iceoryx2 pattern
        var producerTask = Task.Run(() =>
        {
            var writer = channel.Writer;

            while (!benchmarkComplete)
            {
                // TryWrite with spin-wait on failure, matching iceoryx2 pattern
                if (!writer.TryWrite(payload))
                {
                    Thread.SpinWait(1);
                }
            }

            writer.Complete();
        }, cts.Token);

        // Consumer task - counts received messages for real end-to-end throughput
        var consumerTask = Task.Run(() =>
        {
            var reader = channel.Reader;

            while (!benchmarkComplete || reader.TryPeek(out _))
            {
                if (reader.TryRead(out _))
                {
                    // Count received messages (not sent) for real throughput
                    if (warmupComplete)
                    {
                        stats.RecordMessage();
                    }
                }
                else
                {
                    Thread.SpinWait(1);
                }
            }
        }, cts.Token);

        // Warmup phase
        Console.WriteLine($"    Warming up for {_config.WarmupSeconds} seconds...");
        await Task.Delay(TimeSpan.FromSeconds(_config.WarmupSeconds));
        warmupComplete = true;

        // Benchmark phase
        Console.WriteLine($"    Running benchmark for {_config.DurationSeconds} seconds...");
        stats.Start();
        await Task.Delay(TimeSpan.FromSeconds(_config.DurationSeconds));
        stats.Stop();
        benchmarkComplete = true;

        // Cleanup
        cts.Cancel();
        try
        {
            await Task.WhenAll(producerTask, consumerTask);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        return stats;
    }

    /// <summary>
    /// Runs the latency benchmark for the specified payload size.
    /// </summary>
    public async Task<BenchmarkStatistics> RunLatencyAsync(PayloadSize payloadSize)
    {
        var payloadBytes = payloadSize switch
        {
            PayloadSize.Small => 8,
            PayloadSize.Medium => 1024,
            PayloadSize.Large => 65536,
            PayloadSize.ExtraLarge => 524288,
            _ => 8
        };

        return await RunLatencyWithByteArrayAsync(payloadSize, payloadBytes);
    }

    private async Task<BenchmarkStatistics> RunLatencyWithByteArrayAsync(PayloadSize payloadSize, int payloadBytes)
    {
        var stats = new BenchmarkStatistics
        {
            Name = $".NET Channels ({payloadSize})",
            PayloadSize = payloadSize
        };

        var options = new BoundedChannelOptions(_config.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true
        };
        var channel = Channel.CreateBounded<LatencyByteArrayMessage>(options);

        var warmupCount = Math.Max(1000, _config.MessageCount / 10);
        using var cts = new CancellationTokenSource();
        var messageReceived = new TaskCompletionSource<bool>();

        // Consumer task - measures latency on receive
        var consumerTask = Task.Run(async () =>
        {
            var reader = channel.Reader;
            var messagesProcessed = 0;
            var isWarmup = true;

            await foreach (var msg in reader.ReadAllAsync(cts.Token))
            {
                if (isWarmup)
                {
                    messagesProcessed++;
                    if (messagesProcessed >= warmupCount)
                    {
                        isWarmup = false;
                        messagesProcessed = 0;
                    }
                    continue;
                }

                var receiveTime = Stopwatch.GetTimestamp();
                var latencyTicks = receiveTime - msg.SendTimestamp;
                var latencyMicroseconds = latencyTicks * 1_000_000.0 / Stopwatch.Frequency;
                stats.RecordLatency(latencyMicroseconds);

                messagesProcessed++;
                if (messagesProcessed >= _config.MessageCount)
                {
                    messageReceived.TrySetResult(true);
                    break;
                }
            }
        }, cts.Token);

        // Pre-allocate payload
        var payload = new byte[payloadBytes];

        // Producer task - sends messages with timestamps
        var producerTask = Task.Run(async () =>
        {
            var writer = channel.Writer;

            // Warmup
            Console.WriteLine($"    Warming up ({warmupCount:N0} messages)...");
            for (var i = 0; i < warmupCount && !cts.Token.IsCancellationRequested; i++)
            {
                var msg = new LatencyByteArrayMessage
                {
                    SendTimestamp = Stopwatch.GetTimestamp(),
                    Payload = payload
                };
                await writer.WriteAsync(msg, cts.Token);
            }

            // Benchmark
            Console.WriteLine($"    Running latency test ({_config.MessageCount:N0} messages)...");
            stats.Start();
            for (var i = 0; i < _config.MessageCount && !cts.Token.IsCancellationRequested; i++)
            {
                var msg = new LatencyByteArrayMessage
                {
                    SendTimestamp = Stopwatch.GetTimestamp(),
                    Payload = payload
                };
                await writer.WriteAsync(msg, cts.Token);

                // Small delay to prevent overwhelming and to get more realistic latency measurements
                if (i % 100 == 0)
                {
                    await Task.Yield();
                }
            }
            stats.Stop();

            writer.Complete();
        }, cts.Token);

        // Wait for completion or timeout
        var timeout = Task.Delay(TimeSpan.FromSeconds(_config.DurationSeconds * 2 + _config.WarmupSeconds * 2));
        await Task.WhenAny(messageReceived.Task, timeout);

        cts.Cancel();
        try
        {
            await Task.WhenAll(producerTask, consumerTask);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        return stats;
    }

    /// <summary>
    /// Message wrapper that includes send timestamp for latency measurement.
    /// Uses byte array to support large payloads.
    /// </summary>
    private sealed class LatencyByteArrayMessage
    {
        public long SendTimestamp;
        public byte[]? Payload;
    }
}