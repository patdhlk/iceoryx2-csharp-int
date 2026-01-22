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

using Iceoryx2;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceComparison;

/// <summary>
/// Benchmark implementation using iceoryx2 zero-copy IPC.
/// </summary>
public sealed class Iceoryx2Benchmark
{
    private readonly BenchmarkConfig _config;

    public Iceoryx2Benchmark(BenchmarkConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Runs the throughput benchmark for the specified payload size.
    /// </summary>
    public async Task<BenchmarkStatistics> RunThroughputAsync(PayloadSize payloadSize)
    {
        return payloadSize switch
        {
            PayloadSize.Small => await RunThroughputAsync<SmallPayload>(payloadSize, "perf_small"),
            PayloadSize.Medium => await RunThroughputAsync<MediumPayload>(payloadSize, "perf_medium"),
            PayloadSize.Large => await RunThroughputAsync<LargePayload>(payloadSize, "perf_large"),
            _ => throw new ArgumentOutOfRangeException(nameof(payloadSize))
        };
    }

    private async Task<BenchmarkStatistics> RunThroughputAsync<T>(PayloadSize payloadSize, string serviceName)
        where T : unmanaged
    {
        var stats = new BenchmarkStatistics
        {
            Name = $"iceoryx2 ({payloadSize})",
            PayloadSize = payloadSize
        };

        // Create node
        var nodeResult = NodeBuilder.New()
            .Name("benchmark_node")
            .Create();

        if (!nodeResult.IsOk)
        {
            Console.WriteLine($"    ERROR: Failed to create node: {nodeResult}");
            return stats;
        }

        using var node = nodeResult.Unwrap();

        // Create service with appropriate buffer sizes for benchmarking
        var serviceResult = node.ServiceBuilder()
            .PublishSubscribe<T>()
            .MaxPublishers(1)
            .MaxSubscribers(1)
            .SubscriberMaxBufferSize((ulong)_config.ChannelCapacity)
            .EnableSafeOverflow(true)
            .Open(serviceName);

        if (!serviceResult.IsOk)
        {
            Console.WriteLine($"    ERROR: Failed to create service: {serviceResult}");
            return stats;
        }

        using var service = serviceResult.Unwrap();

        // Create publisher
        var publisherResult = service.PublisherBuilder().Create();
        if (!publisherResult.IsOk)
        {
            Console.WriteLine($"    ERROR: Failed to create publisher: {publisherResult}");
            return stats;
        }

        using var publisher = publisherResult.Unwrap();

        // Create subscriber
        var subscriberResult = service.SubscriberBuilder().Create();
        if (!subscriberResult.IsOk)
        {
            Console.WriteLine($"    ERROR: Failed to create subscriber: {subscriberResult}");
            return stats;
        }

        using var subscriber = subscriberResult.Unwrap();

        using var cts = new CancellationTokenSource();
        var warmupComplete = false;
        var benchmarkComplete = false;

        // Producer task
        var producerTask = Task.Run(() =>
        {
            while (!benchmarkComplete)
            {
                var sendResult = publisher.Send(default(T));
                if (!sendResult.IsOk)
                {
                    // Buffer might be full, yield
                    Thread.SpinWait(1);
                    continue;
                }

                if (warmupComplete)
                {
                    stats.RecordMessage();
                }
            }
        }, cts.Token);

        // Consumer task
        var consumerTask = Task.Run(() =>
        {
            while (!benchmarkComplete || !cts.Token.IsCancellationRequested)
            {
                var receiveResult = subscriber.Receive<T>();
                if (!receiveResult.IsOk)
                {
                    break;
                }

                var sample = receiveResult.Unwrap();
                if (sample != null)
                {
                    sample.Dispose();
                }
                else
                {
                    // No sample available, yield briefly
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
        return payloadSize switch
        {
            PayloadSize.Small => await RunLatencyAsync<SmallPayload, LatencySmallPayload>(
                payloadSize, "perf_lat_small"),
            PayloadSize.Medium => await RunLatencyAsync<MediumPayload, LatencyMediumPayload>(
                payloadSize, "perf_lat_medium"),
            PayloadSize.Large => await RunLatencyAsync<LargePayload, LatencyLargePayload>(
                payloadSize, "perf_lat_large"),
            _ => throw new ArgumentOutOfRangeException(nameof(payloadSize))
        };
    }

    private async Task<BenchmarkStatistics> RunLatencyAsync<TPayload, TLatencyPayload>(
        PayloadSize payloadSize,
        string serviceName)
        where TPayload : unmanaged
        where TLatencyPayload : unmanaged, ILatencyPayload
    {
        var stats = new BenchmarkStatistics
        {
            Name = $"iceoryx2 ({payloadSize})",
            PayloadSize = payloadSize
        };

        // Create node
        var nodeResult = NodeBuilder.New()
            .Name("benchmark_latency_node")
            .Create();

        if (!nodeResult.IsOk)
        {
            Console.WriteLine($"    ERROR: Failed to create node: {nodeResult}");
            return stats;
        }

        using var node = nodeResult.Unwrap();

        // Create service
        var serviceResult = node.ServiceBuilder()
            .PublishSubscribe<TLatencyPayload>()
            .MaxPublishers(1)
            .MaxSubscribers(1)
            .SubscriberMaxBufferSize((ulong)_config.ChannelCapacity)
            .EnableSafeOverflow(false) // Don't lose messages for latency test
            .Open(serviceName);

        if (!serviceResult.IsOk)
        {
            Console.WriteLine($"    ERROR: Failed to create service: {serviceResult}");
            return stats;
        }

        using var service = serviceResult.Unwrap();

        // Create publisher
        var publisherResult = service.PublisherBuilder().Create();
        if (!publisherResult.IsOk)
        {
            Console.WriteLine($"    ERROR: Failed to create publisher: {publisherResult}");
            return stats;
        }

        using var publisher = publisherResult.Unwrap();

        // Create subscriber
        var subscriberResult = service.SubscriberBuilder().Create();
        if (!subscriberResult.IsOk)
        {
            Console.WriteLine($"    ERROR: Failed to create subscriber: {subscriberResult}");
            return stats;
        }

        using var subscriber = subscriberResult.Unwrap();

        var warmupCount = Math.Max(1000, _config.MessageCount / 10);
        using var cts = new CancellationTokenSource();
        var messagesReceived = 0;
        var allMessagesReceived = new TaskCompletionSource<bool>();

        // Consumer task
        var consumerTask = Task.Run(() =>
        {
            var isWarmup = true;
            var warmupProcessed = 0;

            while (!cts.Token.IsCancellationRequested)
            {
                var receiveResult = subscriber.Receive<TLatencyPayload>();
                if (!receiveResult.IsOk)
                {
                    break;
                }

                var sample = receiveResult.Unwrap();
                if (sample == null)
                {
                    Thread.SpinWait(1);
                    continue;
                }

                using (sample)
                {
                    if (isWarmup)
                    {
                        warmupProcessed++;
                        if (warmupProcessed >= warmupCount)
                        {
                            isWarmup = false;
                        }
                        continue;
                    }

                    ref readonly var payload = ref sample.GetPayloadRefReadOnly();
                    var receiveTime = Stopwatch.GetTimestamp();
                    var latencyTicks = receiveTime - payload.GetTimestamp();
                    var latencyMicroseconds = latencyTicks * 1_000_000.0 / Stopwatch.Frequency;
                    stats.RecordLatency(latencyMicroseconds);

                    messagesReceived++;
                    if (messagesReceived >= _config.MessageCount)
                    {
                        allMessagesReceived.TrySetResult(true);
                        break;
                    }
                }
            }
        }, cts.Token);

        // Producer - runs synchronously to get accurate timestamps
        // Warmup
        Console.WriteLine($"    Warming up ({warmupCount:N0} messages)...");
        for (var i = 0; i < warmupCount; i++)
        {
            var loanResult = publisher.Loan<TLatencyPayload>();
            if (!loanResult.IsOk)
            {
                Thread.SpinWait(100);
                i--;
                continue;
            }

            using var sample = loanResult.Unwrap();
            ref var payload = ref sample.GetPayloadRef();
            payload.SetTimestamp(Stopwatch.GetTimestamp());
            sample.Send();
        }

        // Wait for warmup to be processed
        await Task.Delay(100);

        // Benchmark
        Console.WriteLine($"    Running latency test ({_config.MessageCount:N0} messages)...");
        stats.Start();
        for (var i = 0; i < _config.MessageCount; i++)
        {
            var loanResult = publisher.Loan<TLatencyPayload>();
            if (!loanResult.IsOk)
            {
                Thread.SpinWait(100);
                i--;
                continue;
            }

            using var sample = loanResult.Unwrap();
            ref var payload = ref sample.GetPayloadRef();
            payload.SetTimestamp(Stopwatch.GetTimestamp());
            sample.Send();

            // Small yield periodically
            if (i % 100 == 0)
            {
                await Task.Yield();
            }
        }
        stats.Stop();

        // Wait for all messages to be received
        var timeout = Task.Delay(TimeSpan.FromSeconds(30));
        await Task.WhenAny(allMessagesReceived.Task, timeout);

        cts.Cancel();
        try
        {
            await consumerTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        return stats;
    }
}

/// <summary>
/// Interface for latency payload types.
/// </summary>
public interface ILatencyPayload
{
    long GetTimestamp();
    void SetTimestamp(long timestamp);
}

/// <summary>
/// Small latency payload with timestamp.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct LatencySmallPayload : ILatencyPayload
{
    public long Timestamp;

    public readonly long GetTimestamp() => Timestamp;
    public void SetTimestamp(long timestamp) => Timestamp = timestamp;
}

/// <summary>
/// Medium latency payload with timestamp.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct LatencyMediumPayload : ILatencyPayload
{
    public long Timestamp;
    public fixed byte Data[1016]; // 1024 - 8 = 1016 bytes

    public readonly long GetTimestamp() => Timestamp;
    public void SetTimestamp(long timestamp) => Timestamp = timestamp;
}

/// <summary>
/// Large latency payload with timestamp.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct LatencyLargePayload : ILatencyPayload
{
    public long Timestamp;
    public fixed byte Data[65528]; // 65536 - 8 = 65528 bytes

    public readonly long GetTimestamp() => Timestamp;
    public void SetTimestamp(long timestamp) => Timestamp = timestamp;
}