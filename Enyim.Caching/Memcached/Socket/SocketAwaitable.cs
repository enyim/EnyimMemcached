﻿// Copyright © 2013 Şafak Gür. All rights reserved.
// Use of this source code is governed by the MIT License (MIT).

namespace Dawn.Net.Sockets
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    ///     Represents awaitable and re-usable socket arguments.
    /// </summary>
    public sealed class SocketAwaitable : IDisposable
    {
        #region Fields
        /// <summary>
        ///     A cached, empty array of bytes.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal static readonly byte[] EmptyArray = new byte[0];

        /// <summary>
        ///     Asynchronous socket arguments for internal use.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SocketAsyncEventArgs arguments = new SocketAsyncEventArgs();

        /// <summary>
        ///     An object that can be used to synchronize access to the
        ///     <see cref="SocketAwaitable" />.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object syncRoot = new object();

        /// <summary>
        ///     An awaiter that waits the completions of asynchronous socket operations.
        /// </summary>
        private readonly SocketAwaiter awaiter;

        /// <summary>
        ///     The data buffer segment that holds the transferred bytes.
        /// </summary>
        private ArraySegment<byte> transferred;

        /// <summary>
        ///     A value indicating whether the <see cref="SocketAwaitable" /> is disposed.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isDisposed;

        /// <summary>
        ///     A value that indicates whether the socket operations using the
        ///     <see cref="SocketAwaitable" /> should capture the current synchronization context
        ///     and attempt to marshall their continuations back to the captured context.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool shouldCaptureContext;
        #endregion

        #region Constructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="SocketAwaitable" /> class.
        /// </summary>
        public SocketAwaitable()
        {
            this.awaiter = new SocketAwaiter(this);
            this.transferred = new ArraySegment<byte>(EmptyArray);
        }
        #endregion

        #region Properties
        /// <summary>
        ///     Gets the socket created for accepting a connection with an asynchronous socket
        ///     method.
        /// </summary>
        public Socket AcceptSocket
        {
            get { return this.Arguments.AcceptSocket; }
        }

        /// <summary>
        ///     Gets or sets the data buffer to use with the asynchronous socket methods.
        /// </summary>
        public ArraySegment<byte> Buffer
        {
            get
            {
                lock (this.syncRoot)
                    return new ArraySegment<byte>(
                        this.Arguments.Buffer ?? EmptyArray,
                        this.Arguments.Offset,
                        this.Arguments.Count);
            }

            set
            {
                lock (this.syncRoot)
                    this.Arguments.SetBuffer(value.Array ?? EmptyArray, value.Offset, value.Count);
            }
        }

        /// <summary>
        ///     Gets the data buffer segment that holds the transferred bytes.
        /// </summary>
        public ArraySegment<byte> Transferred
        {
            get { return this.transferred; }
            internal set { this.transferred = value; }
        }

        /// <summary>
        ///     Gets the exception in the case of a connection failure when a
        ///     <see cref="DnsEndPoint" /> was used.
        /// </summary>
        public Exception ConnectByNameError
        {
            get { return this.Arguments.ConnectByNameError; }
        }
        
        /// <summary>
        ///     Gets the type of socket operation most recently performed with this context object.
        /// </summary>
        public SocketAsyncOperation LastOperation
        {
            get { return this.Arguments.LastOperation; }
        }

        /// <summary>
        ///     Gets or sets the remote IP endpoint for an asynchronous operation.
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get { return this.Arguments.RemoteEndPoint; }
            set { this.Arguments.RemoteEndPoint = value; }
        }

        /// <summary>
        ///     Gets or sets the behavior of an asynchronous operation.
        /// </summary>
        public SocketFlags SocketFlags
        {
            get { return this.Arguments.SocketFlags; }
            set { this.Arguments.SocketFlags = value; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the socket operations using the
        ///     <see cref="SocketAwaitable" /> should capture the current synchronization context
        ///     and attempt to marshall their continuations back to the captured context.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     A socket operation was already in progress using the current
        ///     <see cref="SocketAwaitable" />.
        /// </exception>
        public bool ShouldCaptureContext
        {
            get
            {
                return this.shouldCaptureContext;
            }

            set
            {
                lock (this.awaiter.SyncRoot)
                    if (this.awaiter.IsCompleted)
                        this.shouldCaptureContext = value;
                    else
                        throw new InvalidOperationException(
                            "A socket operation is already in progress"
                            + " using the same awaitable arguments.");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the <see cref="SocketAwaitable" /> is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return this.isDisposed; }
        }

        /// <summary>
        ///     Gets the asynchronous socket arguments for internal use.
        /// </summary>
        internal SocketAsyncEventArgs Arguments
        {
            get { return this.arguments; }
        }
        #endregion

        #region Methods
        /// <summary>
        ///     Clears the buffer, accepted socket, remote endpoint and socket flags to prepare
        ///     <see cref="SocketAwaitable" /> for pooling.
        /// </summary>
        public void Clear()
        {
            this.Arguments.AcceptSocket = null;
            this.Arguments.SetBuffer(EmptyArray, 0, 0);
            this.RemoteEndPoint = null;
            this.SocketFlags = SocketFlags.None;
            this.Transferred = new ArraySegment<byte>(EmptyArray);

            // TODO: Remove with SocketAwaitable.UserToken.
            this.Arguments.UserToken = null;
        }

        /// <summary>
        ///     Gets the awaitable object to await a socket operation.
        /// </summary>
        /// <returns>
        ///     A <see cref="SocketAwaiter" /> used to await this <see cref="SocketAwaitable" />.
        /// </returns>
        public SocketAwaiter GetAwaiter()
        {
            return this.awaiter;
        }

        /// <summary>
        ///     Releases all resources used by <see cref="SocketAwaitable" />.
        /// </summary>
        public void Dispose()
        {
            lock (this.syncRoot)
                if (!this.IsDisposed)
                {
                    this.arguments.Dispose();
                    this.isDisposed = true;
                }
        }
        #endregion
    }
}