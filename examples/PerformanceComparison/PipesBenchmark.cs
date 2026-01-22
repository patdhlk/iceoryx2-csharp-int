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
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceComparison;

/// <summary>
/// Benchmark implementation using .NET System.IO.Pipes (Named Pipes) for IPC comparison.
/// Unlike System.Threading.Channels, Named Pipes support true inter-process communication.
/// </summary>
public sealed class PipesBenchmark
{
    private readonly BenchmarkConfig _config;
    private static int _pipeCounter;

    public PipesBenchmark(BenchmarkConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Runs the throughput benchmark for the specified payload size.
    /// </summary>
    public async Task<BenchmarkStatistics> RunThroughputAsync(PayloadSize payloadSize)
    {
        var payloadBytes = payloadSize switch
        {
            PayloadSize.Small => 8,
            PayloadSize.Medium => 1024,
            PayloadSize.Large => 65536,
            PayloadSize.ExtraLarge => 524288,
            _ => 8
        };

        return await RunThroughputWithBytesAsync(payloadSize, payloadBytes);
    }

    private async Task<BenchmarkStatistics> RunThroughputWithBytesAsync(PayloadSize payloadSize, int payloadBytes)
    {
        var stats = new BenchmarkStatistics
        {
            Name = $"Named Pipes ({payloadSize})",
            PayloadSize = payloadSize
        };

        // Use unique pipe name for each run
        var pipeName = $"perf_benchmark_pipe_{Interlocked.Increment(ref _pipeCounter)}";

        using var cts = new CancellationTokenSource();
        var warmupComplete = false;
        var benchmarkComplete = false;
        var serverReady = new TaskCompletionSource<bool>();

        // Pre-allocate buffers
        var writeBuffer = new byte[payloadBytes];
        var readBuffer = new byte[payloadBytes];

        // Server task (receiver) - counts received messages for real throughput
        var serverTask = Task.Run(async () =>
        {
            await using var server = new NamedPipeServerStream(
                pipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            serverReady.SetResult(true);
            await server.WaitForConnectionAsync(cts.Token);

            while (!benchmarkComplete)
            {
                var bytesRead = 0;
                while (bytesRead < payloadBytes)
                {
                    var read = await server.ReadAsync(
                        readBuffer.AsMemory(bytesRead, payloadBytes - bytesRead),
                        cts.Token);
                    if (read == 0)
                    {
                        // Pipe closed
                        return;
                    }
                    bytesRead += read;
                }

                // Count received messages for real throughput
                if (warmupComplete)
                {
                    stats.RecordMessage();
                }
            }
        }, cts.Token);

        // Wait for server to be ready
        await serverReady.Task;

        // Client task (sender) - sends continuously
        var clientTask = Task.Run(async () =>
        {
            await using var client = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous);

            await client.ConnectAsync(cts.Token);

            while (!benchmarkComplete)
            {
                await client.WriteAsync(writeBuffer, cts.Token);
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
            await Task.WhenAll(serverTask, clientTask);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (IOException)
        {
            // Expected when pipe closes
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

        return await RunLatencyWithBytesAsync(payloadSize, payloadBytes);
    }

    private async Task<BenchmarkStatistics> RunLatencyWithBytesAsync(PayloadSize payloadSize, int payloadBytes)
    {
        var stats = new BenchmarkStatistics
        {
            Name = $"Named Pipes ({payloadSize})",
            PayloadSize = payloadSize
        };

        // Use unique pipe name for each run
        var pipeName = $"perf_latency_pipe_{Interlocked.Increment(ref _pipeCounter)}";

        var warmupCount = Math.Max(1000, _config.MessageCount / 10);
        using var cts = new CancellationTokenSource();
        var serverReady = new TaskCompletionSource<bool>();
        var allMessagesReceived = new TaskCompletionSource<bool>();
        var messagesReceived = 0;

        // Pre-allocate buffers - include timestamp (8 bytes) in the payload
        var writeBuffer = new byte[payloadBytes];
        var readBuffer = new byte[payloadBytes];

        // Server task (receiver) - measures latency on receive
        var serverTask = Task.Run(async () =>
        {
            await using var server = new NamedPipeServerStream(
                pipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            serverReady.SetResult(true);
            await server.WaitForConnectionAsync(cts.Token);

            var isWarmup = true;
            var warmupProcessed = 0;

            while (!cts.Token.IsCancellationRequested)
            {
                var bytesRead = 0;
                while (bytesRead < payloadBytes)
                {
                    var read = await server.ReadAsync(
                        readBuffer.AsMemory(bytesRead, payloadBytes - bytesRead),
                        cts.Token);
                    if (read == 0)
                    {
                        // Pipe closed
                        return;
                    }
                    bytesRead += read;
                }

                if (isWarmup)
                {
                    warmupProcessed++;
                    if (warmupProcessed >= warmupCount)
                    {
                        isWarmup = false;
                    }
                    continue;
                }

                var receiveTime = Stopwatch.GetTimestamp();
                var sendTimestamp = BitConverter.ToInt64(readBuffer, 0);
                var latencyTicks = receiveTime - sendTimestamp;
                var latencyMicroseconds = latencyTicks * 1_000_000.0 / Stopwatch.Frequency;
                stats.RecordLatency(latencyMicroseconds);

                messagesReceived++;
                if (messagesReceived >= _config.MessageCount)
                {
                    allMessagesReceived.TrySetResult(true);
                    break;
                }
            }
        }, cts.Token);

        // Wait for server to be ready
        await serverReady.Task;

        // Client task (sender) - sends messages with timestamps
        await using var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.Out,
            PipeOptions.Asynchronous);

        await client.ConnectAsync(cts.Token);

        // Warmup
        Console.WriteLine($"    Warming up ({warmupCount:N0} messages)...");
        for (var i = 0; i < warmupCount && !cts.Token.IsCancellationRequested; i++)
        {
            BitConverter.TryWriteBytes(writeBuffer, Stopwatch.GetTimestamp());
            await client.WriteAsync(writeBuffer, cts.Token);
        }

        // Wait for warmup messages to be processed
        await Task.Delay(100);

        // Benchmark
        Console.WriteLine($"    Running latency test ({_config.MessageCount:N0} messages)...");
        stats.Start();
        for (var i = 0; i < _config.MessageCount && !cts.Token.IsCancellationRequested; i++)
        {
            BitConverter.TryWriteBytes(writeBuffer, Stopwatch.GetTimestamp());
            await client.WriteAsync(writeBuffer, cts.Token);

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
            await serverTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (IOException)
        {
            // Expected when pipe closes
        }

        return stats;
    }
}