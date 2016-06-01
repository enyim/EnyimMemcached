﻿// Copyright © 2013 Şafak Gür. All rights reserved.
// Use of this source code is governed by the MIT License (MIT).

namespace Dawn.Net.Sockets
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     Represents a buffer manager that when a buffer is requested, blocks the calling thread
    ///     until a buffer is available.
    /// </summary>
    [DebuggerDisplay("Available: {AvailableBuffers} * {BufferSize}B | Disposed: {IsDisposed}")]
    public sealed class BlockingBufferManager : IDisposable
    {
        #region Fields
        /// <summary>
        ///     The full name of the <see cref="BlockingBufferManager" /> type.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string typeName = typeof(BlockingBufferManager).FullName;

        /// <summary>
        ///     Size of the buffers provided by the buffer manager.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int bufferSize;

        /// <summary>
        ///     Data block that provides the underlying storage for the buffers provided by the
        ///     buffer manager.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte[] data;

        /// <summary>
        ///     Zero-based starting indices in <see cref="data" />, of the available segments.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BlockingCollection<int> availableIndices;

        /// <summary>
        ///     Zero-based starting indices in <see cref="data" />, of the unavailable segments.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ConcurrentDictionary<int, int> usedIndices;

        /// <summary>
        ///     A value indicating whether the <see cref="BlockingBufferManager.Dispose" /> has
        ///     been called.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isDisposed;
        #endregion

        #region Constructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="BlockingBufferManager" /> class.
        /// </summary>
        /// <param name="bufferSize">
        ///     Size of the buffers that will be provided by the buffer manager.
        /// </param>
        /// <param name="bufferCount">
        ///     Maximum amount of the buffers that will be concurrently used.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="bufferSize" /> or <paramref name="bufferCount" /> is less than one.
        /// </exception>
        public BlockingBufferManager(int bufferSize, int bufferCount)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(
                    "bufferSize",
                    bufferSize,
                    "Buffer size must not be less than one.");

            if (bufferCount < 1)
                throw new ArgumentOutOfRangeException(
                    "bufferCount",
                    bufferCount,
                    "Buffer count must not be less than one.");

            this.bufferSize = bufferSize;
            this.data = new byte[bufferSize * bufferCount];
            this.availableIndices = new BlockingCollection<int>(bufferCount);
            for (int i = 0; i < bufferCount; i++)
                this.availableIndices.Add(bufferSize * i);

            this.usedIndices = new ConcurrentDictionary<int, int>(bufferCount, bufferCount);
        }
        #endregion

        #region Properties
        /// <summary>
        ///     Gets the size of the buffers provided by the buffer manager.
        /// </summary>
        public int BufferSize
        {
            get { return this.bufferSize; }
        }

        /// <summary>
        ///     Gets the number of available buffers provided by the buffer manager.
        /// </summary>
        public int AvailableBuffers
        {
            get
            {
                lock (this.availableIndices)
                    return !this.isDisposed ? this.availableIndices.Count : 0;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the <see cref="BlockingBufferManager" /> is
        ///     disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return this.isDisposed; }
        }
        #endregion

        #region Methods
        /// <summary>
        ///     Gets an available buffer. This method blocks the calling thread until a buffer
        ///     becomes available.
        /// </summary>
        /// <returns>
        ///     An <see cref="ArraySegment&lt;T&gt;" /> with <see cref="BufferSize" /> as its
        ///     count.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="BlockingBufferManager" /> has been disposed.
        /// </exception>
        public ArraySegment<byte> GetBuffer()
        {
            lock (this.availableIndices)
                if (this.isDisposed)
                    throw new ObjectDisposedException(typeName);

            int index;
            try
            {
                index = this.availableIndices.Take();
            }
            catch (InvalidOperationException)
            {
                throw new ObjectDisposedException(typeName);
            }

            this.usedIndices[index] = index;
            return new ArraySegment<byte>(this.data, index, this.BufferSize);
        }

        /// <summary>
        ///     Releases the specified buffer and makes it available for future use.
        /// </summary>
        /// <param name="buffer">
        ///     Buffer to release.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="buffer" />'s array is null, count is not <see cref="BufferSize" />,
        ///     or the offset is invalid; i.e. not taken from the current buffer manager.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="BlockingBufferManager" /> has been disposed.
        /// </exception>
        public void ReleaseBuffer(ArraySegment<byte> buffer)
        {
            lock (this.availableIndices)
                if (this.isDisposed)
                    throw new ObjectDisposedException(typeName);

            int offset;
            if (buffer.Array != this.data
                || buffer.Count != this.BufferSize
                || !this.usedIndices.TryRemove(buffer.Offset, out offset))
                throw new ArgumentException(
                    "Buffer is not taken from the current buffer manager.",
                    "buffer");

            try
            {
                this.availableIndices.Add(offset);
            }
            catch (InvalidOperationException)
            {
                throw new ObjectDisposedException(typeName);
            }
        }

        /// <summary>
        ///     Releases all resources used by the current instance of
        ///     <see cref="BlockingBufferManager" />. Underlying data block is an exception if it's
        ///     used in unmanaged operations that require pinning the buffer (e.g.
        ///     <see cref="System.Net.Sockets.Socket.ReceiveAsync" />).
        /// </summary>
        [SuppressMessage(
            "Microsoft.Usage",
            "CA2213:DisposableFieldsShouldBeDisposed",
            Justification = "BlockingCollection.Dispose is not thread-safe.",
            MessageId = "availableIndices")]
        public void Dispose()
        {
            lock (this.availableIndices)
                if (!this.isDisposed)
                {
                    this.availableIndices.CompleteAdding();
                    int i;
                    while (this.availableIndices.TryTake(out i))
                        this.usedIndices[i] = i;

                    this.isDisposed = true;
                }
        }
        #endregion
    }
}