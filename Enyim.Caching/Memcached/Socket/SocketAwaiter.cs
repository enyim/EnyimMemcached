// Copyright © 2013 Şafak Gür. All rights reserved.
// Use of this source code is governed by the MIT License (MIT).

namespace Dawn.Net.Sockets
{
    using System;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Provides an object that waits for the completion of a <see cref="SocketAwaitable" />.
    ///     This class is not thread-safe: It doesn't support multiple concurrent awaiters.
    /// </summary>
    [DebuggerDisplay("Completed: {IsCompleted}")]
    public sealed class SocketAwaiter : INotifyCompletion
    {
        #region Fields
        /// <summary>
        ///     A sentinel delegate that does nothing.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Action sentinel = delegate { };

        /// <summary>
        ///     The asynchronous socket arguments to await.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SocketAwaitable awaitable;

        /// <summary>
        ///     An object to synchronize access to the awaiter for validations.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object syncRoot = new object();

        /// <summary>
        ///     The continuation delegate that will be called after the current operation is
        ///     awaited.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Action continuation;

        /// <summary>
        ///     A value indicating whether the asynchronous operation is completed.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isCompleted = true;

        /// <summary>
        ///     A synchronization context for marshaling the continuation delegate to.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SynchronizationContext syncContext;
        #endregion

        #region Constructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="SocketAwaiter" /> class.
        /// </summary>
        /// <param name="awaitable">
        ///     The asynchronous socket arguments to await.
        /// </param>
        internal SocketAwaiter(SocketAwaitable awaitable)
        {
            this.awaitable = awaitable;
            this.awaitable.Arguments.Completed += delegate
            {
                var c = this.continuation
                    ?? Interlocked.CompareExchange(ref this.continuation, sentinel, null);

                if (c != null)
                {
                    var syncContext = this.awaitable.ShouldCaptureContext
                        ? this.SyncContext
                        : null;

                    this.Complete();
                    if (syncContext != null)
                        syncContext.Post(s => c.Invoke(), null);
                    else
                        c.Invoke();
                }
            };
        }
        #endregion

        #region Properties
        /// <summary>
        ///     Gets a value indicating whether the asynchronous operation is completed.
        /// </summary>
        public bool IsCompleted
        {
            get { return this.isCompleted; }
        }

        /// <summary>
        ///     Gets an object to synchronize access to the awaiter for validations.
        /// </summary>
        internal object SyncRoot
        {
            get { return this.syncRoot; }
        }

        /// <summary>
        ///     Gets or sets a synchronization context for marshaling the continuation delegate to.
        /// </summary>
        internal SynchronizationContext SyncContext
        {
            get { return this.syncContext; }
            set { this.syncContext = value; }
        }
        #endregion

        #region Methods
        /// <summary>
        ///     Gets the result of the asynchronous socket operation.
        /// </summary>
        /// <returns>
        ///     A <see cref="SocketError" /> that represents the result of the socket operations.
        /// </returns>
        public SocketError GetResult()
        {
            return this.awaitable.Arguments.SocketError;
        }

        /// <summary>
        ///     Gets invoked when the asynchronous operation is completed and runs the specified
        ///     delegate as continuation.
        /// </summary>
        /// <param name="continuation">
        ///     Continuation to run.
        /// </param>
        void INotifyCompletion.OnCompleted(Action continuation)
        {
            if (this.continuation == sentinel
                || Interlocked.CompareExchange(
                       ref this.continuation,
                       continuation,
                       null) == sentinel)
            {
                this.Complete();
                if (!this.awaitable.ShouldCaptureContext)
                    Task.Run(continuation);
                else
                    Task.Factory.StartNew(
                        continuation,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        /// <summary>
        ///     Resets this awaiter for re-use.
        /// </summary>
        internal void Reset()
        {
            this.awaitable.Arguments.AcceptSocket = null;
            this.awaitable.Arguments.SocketError = SocketError.AlreadyInProgress;
            this.awaitable.Transferred = new ArraySegment<byte>(SocketAwaitable.EmptyArray);
            this.isCompleted = false;
            this.continuation = null;
        }

        /// <summary>
        ///     Sets <see cref="IsCompleted" /> to true, nullifies the <see cref="syncContext" />
        ///     and updates <see cref="SocketAwaitable.Transferred" />.
        /// </summary>
        internal void Complete()
        {
            if (!this.IsCompleted)
            {
                var buffer = this.awaitable.Buffer;
                this.awaitable.Transferred = buffer.Count == 0
                    ? buffer
                    : new ArraySegment<byte>(
                        buffer.Array,
                        buffer.Offset,
                        this.awaitable.Arguments.BytesTransferred);

                if (this.awaitable.ShouldCaptureContext)
                    this.syncContext = null;

                this.isCompleted = true;
            }
        }
        #endregion
    }
}