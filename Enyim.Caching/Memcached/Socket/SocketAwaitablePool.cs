﻿// Copyright © 2013 Şafak Gür. All rights reserved.
// Use of this source code is governed by the MIT License (MIT).

namespace Dawn.Net.Sockets
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    ///     Represents a thread-safe pool of awaitable socket arguments.
    /// </summary>
    [DebuggerDisplay("Count: {Count}")]
    public sealed class SocketAwaitablePool
        : ICollection, IDisposable, IEnumerable<SocketAwaitable>
    {
        #region Fields
        /// <summary>
        ///     The full name of the <see cref="SocketAwaitablePool" /> type.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string typeName = typeof(SocketAwaitablePool).FullName;

        /// <summary>
        ///     A thread-safe, unordered collection of awaitable socket arguments.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ConcurrentBag<SocketAwaitable> bag;

        /// <summary>
        ///     A value indicating whether the <see cref="SocketAwaitablePool" /> is disposed.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isDisposed;
        #endregion

        #region Constructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="SocketAwaitablePool" /> class.
        /// </summary>
        /// <param name="initialCount">
        ///     The initial size of the pool.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="initialCount" /> is less than zero.
        /// </exception>
        public SocketAwaitablePool(int initialCount = 0)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(
                    "initialCount",
                    initialCount,
                    "Initial count must not be less than zero.");

            this.bag = new ConcurrentBag<SocketAwaitable>();
            for (int i = 0; i < initialCount; i++)
                this.Add(new SocketAwaitable());
        }
        #endregion

        #region Properties
        /// <summary>
        ///     Gets the number of awaitable socket arguments in the
        ///     <see cref="SocketAwaitablePool" />.
        /// </summary>
        public int Count
        {
            get
            {
                lock (this.bag)
                    return !this.IsDisposed ? this.bag.Count : 0;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the <see cref="SocketAwaitablePool" /> is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                lock (this.bag)
                    return this.isDisposed;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether access to the <see cref="ICollection" /> is
        ///     synchronized with the <see cref="ICollection.SyncRoot" /> property.
        ///     This property always returns false.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        ///     Gets an object that can be used to synchronize access to the
        ///     <see cref="ICollection" />. This property is not supported.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot
        {
            get { throw new NotSupportedException(
                "Synchronization using SyncRoot is not supported."); }
        }
        #endregion

        #region Methods
        /// <summary>
        ///     Adds a <see cref="SocketAwaitable" /> instance to the pool.
        /// </summary>
        /// <param name="awaitable">
        ///     Awaitable socket arguments to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="awaitable" /> is null.
        /// </exception>
        public void Add(SocketAwaitable awaitable)
        {
            if (awaitable == null)
                throw new ArgumentNullException(
                    "awaitable",
                    "Awaitable socket arguments to pull must not be null.");

            lock (this.bag)
                if (!this.IsDisposed)
                    this.bag.Add(awaitable);
                else
                    awaitable.Dispose();
        }

        /// <summary>
        ///     Removes and returns a <see cref="SocketAwaitable" /> instance from the pool, if the
        ///     pool has one; otherwise, returns a new <see cref="SocketAwaitable" /> instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="SocketAwaitable" /> instance from the pool, if the pool has one;
        ///     otherwise, a new <see cref="SocketAwaitable" /> instance.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="SocketAwaitablePool" /> has been disposed.
        /// </exception>
        public SocketAwaitable Take()
        {
            SocketAwaitable awaitable;
            lock (this.bag)
                if (!this.IsDisposed)
                    return this.bag.TryTake(out awaitable) ? awaitable : new SocketAwaitable();
                else
                    throw new ObjectDisposedException(typeName);
        }

        /// <summary>
        ///     Copies the pool elements to an existing one-dimensional array, starting at the
        ///     specified offset.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional array of awaitable socket arguments that is the destination of
        ///     the arguments copied from the pool. Array must have zero-based indexing.
        /// </param>
        /// <param name="offset">
        ///     The zero-based index in <paramref name="array" /> of which copying begins.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="array" /> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="offset" /> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="array" /> is not a single-dimensional array of
        ///     <see cref="SocketAwaitable" /> instances.
        ///     -or-
        ///     <paramref name="offset" /> is equal to or greater than the length of
        ///     <paramref name="array" />
        ///     -or-
        ///     The number of elements in the source pool is greater than the available space from
        ///     <paramref name="offset" /> to the end of <paramref name="array" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="SocketAwaitablePool" /> has been disposed.
        /// </exception>
        void ICollection.CopyTo(Array array, int offset)
        {
            if (array == null)
                throw new ArgumentNullException("array", "Array must not be null.");

            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", offset, "Index must not be null.");

            if (!(array is SocketAwaitable[]))
            {
                var message = string.Format(
                    "Array must be a single-dimensional array of `{0}`.",
                    typeof(SocketAwaitable).FullName);

                throw new ArgumentException(message, "array");
            }

            lock (this.bag)
                if (!this.IsDisposed)
                    this.bag.CopyTo(array as SocketAwaitable[], offset);
                else
                    throw new ObjectDisposedException(typeName);
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the <see cref="SocketAwaitablePool" />.
        /// </summary>
        /// <returns>
        ///     An enumerator for the contents of the <see cref="SocketAwaitablePool" />.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="SocketAwaitablePool" /> has been disposed.
        /// </exception>
        public IEnumerator<SocketAwaitable> GetEnumerator()
        {
            if (!this.IsDisposed)
                return this.bag.GetEnumerator();
            else
                throw new ObjectDisposedException(typeName);
        }

        /// <summary>
        ///     Returns a non-generic enumerator that iterates through the
        ///     <see cref="SocketAwaitablePool" />.
        /// </summary>
        /// <returns>
        ///     An enumerator for the contents of the <see cref="SocketAwaitablePool" />.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="SocketAwaitablePool" /> has been disposed.
        /// </exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        ///     Release all resources used by the <see cref="SocketAwaitablePool" />.
        /// </summary>
        public void Dispose()
        {
            lock (this.bag)
                if (!this.IsDisposed)
                {
                    for (int i = 0; i < this.Count; i++)
                        this.Take().Dispose();

                    this.isDisposed = true;
                }
        }
        #endregion
    }
}
