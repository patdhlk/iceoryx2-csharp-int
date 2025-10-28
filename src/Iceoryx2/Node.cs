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

using Iceoryx2.SafeHandles;
using System;

namespace Iceoryx2;

/// <summary>
/// Represents a node in the Iceoryx2 system.
/// The node serves as a central entry point and is linked to a specific process within the Iceoryx2 ecosystem.
/// It provides capabilities for creating or opening services and managing node-specific resources.
/// </summary>
public sealed class Node : IDisposable
{
    internal SafeNodeHandle _handle;
    private bool _disposed;

    internal Node(SafeNodeHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public string Name
    {
        get
        {
            ThrowIfDisposed();
            // TODO: Implement proper node name retrieval
            return "node"; // Placeholder
        }
    }

    /// <summary>
    /// Gets the unique ID of the node.
    /// </summary>
    public Guid Id
    {
        get
        {
            ThrowIfDisposed();
            // TODO: Implement proper node ID retrieval
            return Guid.NewGuid(); // Placeholder
        }
    }

    /// <summary>
    /// Creates a builder for creating or opening a service.
    /// </summary>
    public ServiceBuilder ServiceBuilder()
    {
        ThrowIfDisposed();
        return new ServiceBuilder(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the Node and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _handle?.Dispose();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Node));
    }
}