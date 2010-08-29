using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Enyim.Caching.Memcached
{
	[DebuggerDisplay("[ Address: {endpoint}, IsAlive = {IsAlive} ]")]
	public class PooledSocket : IDisposable
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(PooledSocket));

		private bool isAlive = true;
		private Socket socket;
		private IPEndPoint endpoint;

		private BufferedStream inputStream;

		public PooledSocket(IPEndPoint endpoint, TimeSpan connectionTimeout, TimeSpan receiveTimeout)
		{
			var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			// all operations are "atomic", we do not send small chunks of data
			socket.NoDelay = true;

			var mre = new ManualResetEvent(false);
			var timeout = connectionTimeout == TimeSpan.MaxValue
							? Timeout.Infinite
							: (int)connectionTimeout.TotalMilliseconds;

			socket.BeginConnect(endpoint, iar =>
			{
				try { using (iar.AsyncWaitHandle) socket.EndConnect(iar); }
				catch { }

				mre.Set();
			}, null);

			if (!mre.WaitOne(timeout) || !socket.Connected)
			{
				using (socket)
					throw new TimeoutException("Could not connect to " + endpoint);
			}

			this.socket = socket;
			this.endpoint = endpoint;

			this.inputStream = new BufferedStream(new BasicNetworkStream(socket));
		}

		public Action<PooledSocket> CleanupCallback { get; set; }

		public int Available
		{
			get { return this.socket.Available; }
		}

		public void Reset()
		{
			//this.LockToThread();

			// discard any buffered data
			this.inputStream.Flush();

			int available = this.socket.Available;

			if (available > 0)
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Socket bound to {0} has {1} unread data! This is probably a bug in the code. InstanceID was {2}.", this.socket.RemoteEndPoint, available, this.InstanceId);

				byte[] data = new byte[available];

				this.Read(data, 0, available);

				if (log.IsWarnEnabled)
					log.Warn(Encoding.ASCII.GetString(data));
			}

			if (log.IsDebugEnabled)
				log.DebugFormat("Socket {0} was reset", this.InstanceId);
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
						try { this.socket.Close(); }
						catch { }

					if (this.inputStream != null)
						this.inputStream.Dispose();

					this.inputStream = null;
					this.socket = null;
					this.CleanupCallback = null;
				}
				catch (Exception e)
				{
					log.Error(e);
				}
			}
			else
			{
				Action<PooledSocket> cc = this.CleanupCallback;

				if (cc != null)
				{
					cc(this);
				}
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

			this.socket.Send(buffers, SocketFlags.None, out status);

			if (status != SocketError.Success)
			{
				this.isAlive = false;

				ThrowHelper.ThrowSocketWriteError(this.endpoint, status);
			}
		}

		public IAsyncResult BeginWrite(IList<ArraySegment<byte>> buffers, AsyncCallback callback, object state)
		{
			this.CheckDisposed();

			return this.socket.BeginSend(buffers, SocketFlags.None, callback, state);
		}

		public void EndWrite(IAsyncResult result)
		{
			SocketError status;

			this.socket.EndSend(result, out status);

			if (status != SocketError.Success)
			{
				this.isAlive = false;

				ThrowHelper.ThrowSocketWriteError(this.endpoint, status);
			}
		}

		//public IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		//{
		//    this.inputStream.BEG
		//    return null;

		//    //return this.socket.BeginReceive(buffer, offset, size, callback, state);

		//    //            this.CheckDisposed();

		//    //int read = 0;
		//    //int shouldRead = count;

		//    //while (read < count)
		//    //{
		//    //    try
		//    //    {
		//    //        int currentRead = this.inputStream.Read(buffer, offset, shouldRead);
		//    //        if (currentRead < 1)
		//    //            continue;

		//    //        read += currentRead;
		//    //        offset += currentRead;
		//    //        shouldRead -= currentRead;
		//    //    }
		//    //    catch (IOException)
		//    //    {
		//    //        this.isAlive = false;
		//    //        throw;
		//    //    }
		//    //}

		//}


		#region [ BasicNetworkStream           ]
		private class BasicNetworkStream : Stream
		{
			private Socket socket;

			public BasicNetworkStream(Socket socket)
			{
				this.socket = socket;
			}

			public override bool CanRead
			{
				get { return true; }
			}

			public override bool CanSeek
			{
				get { return false; }
			}

			public override bool CanWrite
			{
				get { return false; }
			}

			public override void Flush()
			{
			}

			public override long Length
			{
				get { throw new NotSupportedException(); }
			}

			public override long Position
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				SocketError errorCode;

				var retval = this.socket.BeginReceive(buffer, offset, count, SocketFlags.None, out errorCode, callback, state);

				if (errorCode == SocketError.Success)
					return retval;

				throw new System.IO.IOException(String.Format("Failed to read from the socket '{0}'. Error: {1}", this.socket.RemoteEndPoint, errorCode));
			}

			public override int EndRead(IAsyncResult asyncResult)
			{
				SocketError errorCode;

				var retval = this.socket.EndReceive(asyncResult, out errorCode);

				// actually "0 bytes read" could mean an error as well
				if (errorCode == SocketError.Success && retval > 0)
					return retval;

				throw new System.IO.IOException(String.Format("Failed to read from the socket '{0}'. Error: {1}", this.socket.RemoteEndPoint, errorCode));
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				SocketError errorCode;

				int retval = this.socket.Receive(buffer, offset, count, SocketFlags.None, out errorCode);

				// actually "0 bytes read" could mean an error as well
				if (errorCode == SocketError.Success && retval > 0)
					return retval;

				throw new System.IO.IOException(String.Format("Failed to read from the socket '{0}'. Error: {1}", this.socket.RemoteEndPoint, errorCode));
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotSupportedException();
			}

			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}
		}
		#endregion
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kiskó, enyim.com
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
