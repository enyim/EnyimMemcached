using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Enyim.Caching.Memcached.Operations.Text
{
	internal static class TextSocketHelper
	{
		private const string GenericErrorResponse = "ERROR";
		private const string ClientErrorResponse = "CLIENT_ERROR ";
		private const string ServerErrorResponse = "SERVER_ERROR ";
		private const int ErrorResponseLength = 13;

		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(TextSocketHelper));

		/// <summary>
		/// Reads the response of the server.
		/// </summary>
		/// <returns>The data sent by the memcached server.</returns>
		/// <exception cref="T:System.InvalidOperationException">The server did not sent a response or an empty line was returned.</exception>
		/// <exception cref="T:Enyim.Caching.Memcached.MemcachedException">The server did not specified any reason just returned the string ERROR. - or - The server returned a SERVER_ERROR, in this case the Message of the exception is the message returned by the server.</exception>
		/// <exception cref="T:Enyim.Caching.Memcached.MemcachedClientException">The server did not recognize the request sent by the client. The Message of the exception is the message returned by the server.</exception>
		public static string ReadResponse(PooledSocket socket)
		{
			string response = TextSocketHelper.ReadLine(socket);

			if (log.IsDebugEnabled)
				log.Debug("Received response: " + response);

			if (String.IsNullOrEmpty(response))
				throw new MemcachedClientException("Empty response received.");

			if (String.Compare(response, GenericErrorResponse, StringComparison.Ordinal) == 0)
				throw new NotSupportedException("Operation is not supported by the server or the request was malformed. If the latter please report the bug to the developers.");

			if (response.Length >= ErrorResponseLength)
			{
				if (String.Compare(response, 0, ClientErrorResponse, 0, ErrorResponseLength, StringComparison.Ordinal) == 0)
				{
					throw new MemcachedClientException(response.Remove(0, ErrorResponseLength));
				}
				else if (String.Compare(response, 0, ServerErrorResponse, 0, ErrorResponseLength, StringComparison.Ordinal) == 0)
				{
					throw new MemcachedException(response.Remove(0, ErrorResponseLength));
				}
			}

			return response;
		}


		/// <summary>
		/// Reads a line from the socket. A line is terninated by \r\n.
		/// </summary>
		/// <returns></returns>
		private static string ReadLine(PooledSocket socket)
		{
			MemoryStream ms = new MemoryStream(50);

			bool gotR = false;
			byte[] buffer = new byte[1];

			int data;

				while (true)
				{
					data = socket.ReadByte();

					if (data == 13)
					{
						gotR = true;
						continue;
					}

					if (gotR)
					{
						if (data == 10)
							break;

						ms.WriteByte(13);

						gotR = false;
					}

					ms.WriteByte((byte)data);
				}

			string retval = Encoding.ASCII.GetString(ms.GetBuffer(), 0, (int)ms.Length);

			if (log.IsDebugEnabled)
				log.Debug("ReadLine: " + retval);

			return retval;
		}
	}
}
