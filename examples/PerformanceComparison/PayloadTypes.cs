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

using System.Runtime.InteropServices;

namespace PerformanceComparison;

/// <summary>
/// Small payload: 8 bytes
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SmallPayload
{
    public long Value;
}

/// <summary>
/// Medium payload: 1024 bytes (1 KB)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct MediumPayload
{
    public long Timestamp;
    public long SequenceNumber;
    public fixed byte Data[1008]; // 1024 - 16 = 1008 bytes
}

/// <summary>
/// Large payload: 65536 bytes (64 KB)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct LargePayload
{
    public long Timestamp;
    public long SequenceNumber;
    public fixed byte Data[65520]; // 65536 - 16 = 65520 bytes
}

/// <summary>
/// Extra large payload: 524288 bytes (512 KB)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ExtraLargePayload
{
    public long Timestamp;
    public long SequenceNumber;
    public fixed byte Data[524272]; // 524288 - 16 = 524272 bytes
}

/// <summary>
/// Payload size enumeration for benchmark configuration.
/// </summary>
public enum PayloadSize
{
    Small,      // 8 bytes
    Medium,     // 1 KB
    Large,      // 64 KB
    ExtraLarge  // 512 KB
}

/// <summary>
/// Benchmark mode enumeration.
/// </summary>
public enum BenchmarkMode
{
    Throughput,
    Latency
}

/// <summary>
/// Target system for benchmarking.
/// </summary>
public enum BenchmarkTarget
{
    Channels,
    Iceoryx2,
    Pipes,
    Both,
    All
}