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

namespace Enyim.Caching.Memcached
{
    [DebuggerDisplay("[ Address: {endpoint}, IsAlive = {IsAlive} ]")]
    public partial class PooledSocket : IDisposable
    {
        private readonly ILogger _logger;

        private bool isAlive;
        private Socket socket;
        private EndPoint endpoint;

        private Stream inputStream;
        private AsyncSocketHelper helper;

        public PooledSocket(DnsEndPoint endpoint, TimeSpan connectionTimeout, TimeSpan receiveTimeout, ILogger logger)
        {
            _logger = logger;

            this.isAlive = true;

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);         
            socket.NoDelay = true;

            var timeout = connectionTimeout == TimeSpan.MaxValue
                            ? Timeout.Infinite
                            : (int)connectionTimeout.TotalMilliseconds;

            var rcv = receiveTimeout == TimeSpan.MaxValue
                ? Timeout.Infinite
                : (int)receiveTimeout.TotalMilliseconds;

            socket.ReceiveTimeout = rcv;
            socket.SendTimeout = rcv;            

            ConnectWithTimeout(socket, endpoint, timeout);

            this.socket = socket;
            this.endpoint = endpoint;

            this.inputStream = new NetworkStream(socket);            
        }

        private void ConnectWithTimeout(Socket socket, DnsEndPoint endpoint, int timeout)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            var args = new SocketAsyncEventArgs();

            //Workaround for https://github.com/dotnet/corefx/issues/26840
            if (!IPAddress.TryParse(endpoint.Host, out var address))
            {
                address = Dns.GetHostAddresses(endpoint.Host).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                if (address == null)
                    throw new ArgumentException(String.Format("Could not resolve host '{0}'.", endpoint.Host));
                args.RemoteEndPoint = new IPEndPoint(address, endpoint.Port);
            }
            else
            {
                //DnsEndPoint is not working on linux
                args.RemoteEndPoint = new IPEndPoint(address, endpoint.Port);
            }

            using (var mres = new ManualResetEventSlim())
            {
                args.Completed += (s, e) => mres.Set();
                if (socket.ConnectAsync(args))
                {
                    if(!mres.Wait(timeout))
                    {
                        throw new TimeoutException("Could not connect to " + endpoint);
                    }
                }
            }           
        }        

        public Action<PooledSocket> CleanupCallback { get; set; }

        public int Available
        {
            get { return this.socket.Available; }
        }

        public void Reset()
        {
            // discard any buffered data
            this.inputStream.Flush();

            if (this.helper != null) this.helper.DiscardBuffer();

            int available = this.socket.Available;

            if (available > 0)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("Socket bound to {0} has {1} unread data! This is probably a bug in the code. InstanceID was {2}.", this.socket.RemoteEndPoint, available, this.InstanceId);

                byte[] data = new byte[available];

                this.Read(data, 0, available);

                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning(Encoding.ASCII.GetString(data));
            }

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Socket {0} was reset", this.InstanceId);
        }

        /// <summary>
        /// The ID of this instance. Used by the <see cref="T:MemcachedServer"/> to identify the instance in its inner lists.
        /// </summary>
        public readonly Guid InstanceId = Guid.NewGuid();

        public bool IsAlive
        {
            get { return this.isAlive; }
        }

        /// <summary>
        /// Releases all resources used by this instance and shuts down the inner <see cref="T:Socket"/>. This instance will not be usable anymore.
        /// </summary>
        /// <remarks>Use the IDisposable.Dispose method if you want to release this instance back into the pool.</remarks>
        public void Destroy()
        {
            this.Dispose(true);
        }

        ~PooledSocket()
        {
            try { this.Dispose(true); }
            catch { }
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);

                try
                {
                    if (socket != null)
                        try { this.socket.Dispose(); }
                        catch { }

                    if (this.inputStream != null)
                        this.inputStream.Dispose();

                    this.inputStream = null;
                    this.socket = null;
                    this.CleanupCallback = null;
                }
                catch (Exception e)
                {
                    _logger.LogError(nameof(PooledSocket), e);
                }
            }
            else
            {
                Action<PooledSocket> cc = this.CleanupCallback;

                if (cc != null)
                    cc(this);
            }
        }

        void IDisposable.Dispose()
        {
            this.Dispose(false);
        }

        private void CheckDisposed()
        {
            if (this.socket == null)
                throw new ObjectDisposedException("PooledSocket");
        }

        /// <summary>
        /// Reads the next byte from the server's response.
        /// </summary>
        /// <remarks>This method blocks and will not return until the value is read.</remarks>
        public int ReadByte()
        {
            this.CheckDisposed();

            try
            {
                return this.inputStream.ReadByte();
            }
            catch (IOException)
            {
                this.isAlive = false;

                throw;
            }
        }

        public async Task<byte[]> ReadBytesAsync(int count)
        {
            using (var awaitable = new SocketAwaitable())
            {
                awaitable.Buffer = new ArraySegment<byte>(new byte[count], 0, count);
                await this.socket.ReceiveAsync(awaitable);
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
            this.CheckDisposed();

            int read = 0;
            int shouldRead = count;

            while (read < count)
            {
                try
                {
                    int currentRead = this.inputStream.Read(buffer, offset, shouldRead);
                    if (currentRead < 1)
                        continue;

                    read += currentRead;
                    offset += currentRead;
                    shouldRead -= currentRead;
                }
                catch (IOException)
                {
                    this.isAlive = false;
                    throw;
                }
            }
        }

        public void Write(byte[] data, int offset, int length)
        {
            this.CheckDisposed();

            SocketError status;

            this.socket.Send(data, offset, length, SocketFlags.None, out status);

            if (status != SocketError.Success)
            {
                this.isAlive = false;

                ThrowHelper.ThrowSocketWriteError(this.endpoint, status);
            }
        }

        public void Write(IList<ArraySegment<byte>> buffers)
        {
            this.CheckDisposed();

            SocketError status;

#if DEBUG
            int total = 0;
            for (int i = 0, C = buffers.Count; i < C; i++)
                total += buffers[i].Count;

            if (this.socket.Send(buffers, SocketFlags.None, out status) != total)
                System.Diagnostics.Debugger.Break();
#else
            this.socket.Send(buffers, SocketFlags.None, out status);
#endif

            if (status != SocketError.Success)
            {
                this.isAlive = false;

                ThrowHelper.ThrowSocketWriteError(this.endpoint, status);
            }
        }

        public async Task WriteSync(IList<ArraySegment<byte>> buffers)
        {
            using (var awaitable = new SocketAwaitable())
            {
                awaitable.Arguments.BufferList = buffers;
                try
                {
                    await this.socket.SendAsync(awaitable);
                }
                catch
                {
                    this.isAlive = false;
                    ThrowHelper.ThrowSocketWriteError(this.endpoint, awaitable.Arguments.SocketError);
                }

                if (awaitable.Arguments.SocketError != SocketError.Success)
                {
                    this.isAlive = false;
                    ThrowHelper.ThrowSocketWriteError(this.endpoint, awaitable.Arguments.SocketError);
                }
            }
        }

        /// <summary>
        /// Receives data asynchronously. Returns true if the IO is pending. Returns false if the socket already failed or the data was available in the buffer.
        /// p.Next will only be called if the call completes asynchronously.
        /// </summary>
        public bool ReceiveAsync(AsyncIOArgs p)
        {
            this.CheckDisposed();

            if (!this.IsAlive)
            {
                p.Fail = true;
                p.Result = null;

                return false;
            }

            if (this.helper == null)
                this.helper = new AsyncSocketHelper(this);

            return this.helper.Read(p);
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
