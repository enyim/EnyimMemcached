//#define DEBUG_IO
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Dawn.Net.Sockets;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Enyim.Caching.Memcached
{
    [DebuggerDisplay("[ Address: {endpoint}, IsAlive = {IsAlive} ]")]
    public partial class AsyncPooledSocket : IDisposable
    {
        private readonly ILogger _logger;
        private bool _isAlive;
        private Socket _socket;
        private Stream _inputStream;
        private AsyncSocketHelper _helper;

        public AsyncPooledSocket(ILogger logger)
        {
            _logger = logger;
            _isAlive = true;            
        }

        private async Task CreateSocketAsync(DnsEndPoint endpoint, TimeSpan connectionTimeout, TimeSpan receiveTimeout)
        {
            CancellationTokenSource cancellationConnTimeout = null;
            if (connectionTimeout != TimeSpan.MaxValue)
            {
                cancellationConnTimeout = new CancellationTokenSource(connectionTimeout);
            }

            var args = new ConnectEventArgs();
            args.RemoteEndPoint = endpoint;

            if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args))
            {
                if (cancellationConnTimeout != null)
                {
                    using (cancellationConnTimeout.Token.Register(s => Socket.CancelConnectAsync((SocketAsyncEventArgs)s), args))
                    {
                        await args.Builder.Task.ConfigureAwait(false);
                    }
                }
            }
            else if (args.SocketError != SocketError.Success)
            {
                throw new SocketException((int)args.SocketError);
            }

            _socket = args.ConnectSocket;
            _socket.ReceiveTimeout = receiveTimeout == TimeSpan.MaxValue
                ? Timeout.Infinite
                : (int)receiveTimeout.TotalMilliseconds;
            _socket.NoDelay = true;
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            _inputStream = new NetworkStream(_socket, ownsSocket: true);        
        }

        //From https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/ConnectHelper.cs
        private sealed class ConnectEventArgs : SocketAsyncEventArgs
        {
            public AsyncTaskMethodBuilder Builder { get; private set; }
            public CancellationToken CancellationToken { get; private set; }

            public void Initialize(CancellationToken cancellationToken)
            {
                CancellationToken = cancellationToken;
                var b = new AsyncTaskMethodBuilder();
                var ignored = b.Task; 
                Builder = b;
            }

            protected override void OnCompleted(SocketAsyncEventArgs _)
            {
                switch (SocketError)
                {
                    case SocketError.Success:
                        Builder.SetResult();
                        break;

                    case SocketError.OperationAborted:
                    case SocketError.ConnectionAborted:
                        if (CancellationToken.IsCancellationRequested)
                        {
                            Builder.SetException(new TaskCanceledException());
                            break;
                        }
                        goto default;

                    default:
                        Builder.SetException(new SocketException((int)SocketError));
                        break;
                }
            }
        }        

        public Action<AsyncPooledSocket> CleanupCallback { get; set; }

        public int Available
        {
            get { return _socket.Available; }
        }

        public void Reset()
        {
            // discard any buffered data
            _inputStream.Flush();

            if (_helper != null) _helper.DiscardBuffer();

            int available = _socket.Available;

            if (available > 0)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("Socket bound to {0} has {1} unread data! This is probably a bug in the code. InstanceID was {2}.", _socket.RemoteEndPoint, available, InstanceId);

                byte[] data = new byte[available];

                Read(data, 0, available);

                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning(Encoding.ASCII.GetString(data));
            }

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Socket {0} was reset", InstanceId);
        }

        /// <summary>
        /// The ID of this instance. Used by the <see cref="T:MemcachedServer"/> to identify the instance in its inner lists.
        /// </summary>
        public readonly Guid InstanceId = Guid.NewGuid();

        public bool IsAlive
        {
            get { return _isAlive; }
        }

        /// <summary>
        /// Releases all resources used by this instance and shuts down the inner <see cref="T:Socket"/>. This instance will not be usable anymore.
        /// </summary>
        /// <remarks>Use the IDisposable.Dispose method if you want to release this instance back into the pool.</remarks>
        public void Destroy()
        {
            Dispose(true);
        }

        ~AsyncPooledSocket()
        {
            try { Dispose(true); }
            catch { }
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);

                try
                {
                    if (_socket != null)
                        try { _socket.Dispose(); }
                        catch { }

                    if (_inputStream != null)
                        _inputStream.Dispose();

                    _inputStream = null;
                    _socket = null;
                    CleanupCallback = null;
                }
                catch (Exception e)
                {
                    _logger.LogError(nameof(PooledSocket), e);
                }
            }
            else
            {
                Action<AsyncPooledSocket> cc = CleanupCallback;

                if (cc != null)
                    cc(this);
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(false);
        }

        private void CheckDisposed()
        {
            if (_socket == null)
                throw new ObjectDisposedException("PooledSocket");
        }

        /// <summary>
        /// Reads the next byte from the server's response.
        /// </summary>
        /// <remarks>This method blocks and will not return until the value is read.</remarks>
        public int ReadByte()
        {
            CheckDisposed();

            try
            {
                return _inputStream.ReadByte();
            }
            catch (IOException)
            {
                _isAlive = false;

                throw;
            }
        }

        public async Task<byte[]> ReadBytesAsync(int count)
        {
            using (var awaitable = new SocketAwaitable())
            {
                awaitable.Buffer = new ArraySegment<byte>(new byte[count], 0, count);
                await _socket.ReceiveAsync(awaitable);
                return awaitable.Transferred.Array;
            }
        }

        /// <summary>
        /// Reads data from the server into the specified buffer.
        /// </summary>
        /// <param name="buffer">An array of <see cref="T:System.Byte"/> that is the storage location for the received data.</param>
        /// <param name="offset">The location in buffer to store the received data.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <remarks>This method blocks and will not return until the specified amount of bytes are read.</remarks>
        public void Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            int read = 0;
            int shouldRead = count;

            while (read < count)
            {
                try
                {
                    int currentRead = _inputStream.Read(buffer, offset, shouldRead);
                    if (currentRead < 1)
                        continue;

                    read += currentRead;
                    offset += currentRead;
                    shouldRead -= currentRead;
                }
                catch (IOException)
                {
                    _isAlive = false;
                    throw;
                }
            }
        }

        public void Write(byte[] data, int offset, int length)
        {
            CheckDisposed();

            SocketError status;

            _socket.Send(data, offset, length, SocketFlags.None, out status);

            if (status != SocketError.Success)
            {
                _isAlive = false;

                ThrowHelper.ThrowSocketWriteError(_socket.RemoteEndPoint, status);
            }
        }

        public void Write(IList<ArraySegment<byte>> buffers)
        {
            CheckDisposed();

            SocketError status;

#if DEBUG
            int total = 0;
            for (int i = 0, C = buffers.Count; i < C; i++)
                total += buffers[i].Count;

            if (_socket.Send(buffers, SocketFlags.None, out status) != total)
                System.Diagnostics.Debugger.Break();
#else
            _socket.Send(buffers, SocketFlags.None, out status);
#endif

            if (status != SocketError.Success)
            {
                _isAlive = false;

                ThrowHelper.ThrowSocketWriteError(_socket.RemoteEndPoint, status);
            }
        }

        public async Task WriteSync(IList<ArraySegment<byte>> buffers)
        {
            using (var awaitable = new SocketAwaitable())
            {
                awaitable.Arguments.BufferList = buffers;
                try
                {
                    await _socket.SendAsync(awaitable);
                }
                catch
                {
                    _isAlive = false;
                    ThrowHelper.ThrowSocketWriteError(_socket.RemoteEndPoint, awaitable.Arguments.SocketError);
                }

                if (awaitable.Arguments.SocketError != SocketError.Success)
                {
                    _isAlive = false;
                    ThrowHelper.ThrowSocketWriteError(_socket.RemoteEndPoint, awaitable.Arguments.SocketError);
                }
            }
        }

        /// <summary>
        /// Receives data asynchronously. Returns true if the IO is pending. Returns false if the socket already failed or the data was available in the buffer.
        /// p.Next will only be called if the call completes asynchronously.
        /// </summary>
        public bool ReceiveAsync(AsyncIOArgs p)
        {
            CheckDisposed();

            if (!_isAlive)
            {
                p.Fail = true;
                p.Result = null;

                return false;
            }

            if (_helper == null)
                _helper = new AsyncSocketHelper(this);

            return _helper.Read(p);
        }
    }
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk? enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
