using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Threading;

namespace Enyim
{
	/// <summary>
	/// Implements a 64 bit long Fowler-Noll-Vo hash.
	/// </summary>
	/// <remarks>
	/// Calculation found at http://lists.danga.com/pipermail/memcached/2007-April/003846.html, but 
	/// it is pretty much available everywhere
	/// </remarks>
	public sealed class FNV64 : System.Security.Cryptography.HashAlgorithm
	{
		private const ulong FNV_64_INIT = 0xcbf29ce484222325L;
		private const ulong FNV_64_PRIME = 0x100000001b3L;

		private ulong currentHashValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FNV64"/> class.
		/// </summary>
		public FNV64()
		{
			base.HashSizeValue = 64;

			this.Initialize();
		}

		/// <summary>
		/// Initializes an instance of <see cref="T:FNV64"/>.
		/// </summary>
		public override void Initialize()
		{
			this.currentHashValue = FNV_64_INIT;
		}

		/// <summary>Routes data written to the object into the <see cref="T:FNV64" /> hash algorithm for computing the hash.</summary>
		/// <param name="array">The input data. </param>
		/// <param name="ibStart">The offset into the byte array from which to begin using data. </param>
		/// <param name="cbSize">The number of bytes in the array to use as data. </param>
		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			int end = ibStart + cbSize;

			for (int i = ibStart; i < end; i++)
			{
				this.currentHashValue = (this.currentHashValue * FNV_64_PRIME) ^ array[i];
			}
		}

		/// <summary>
		/// Returns the computed <see cref="T:FNV64" /> hash value after all data has been written to the object.
		/// </summary>
		/// <returns>The computed hash code.</returns>
		protected override byte[] HashFinal()
		{
			return BitConverter.GetBytes(this.currentHashValue);
		}
	}

	/// <summary>
	/// Implements an FNV1a hash algorithm.
	/// </summary>
	public class FNV1a : HashAlgorithm
	{
		private const uint Prime = 16777619;
		private const uint Offset = 2166136261;

		/// <summary>
		/// The current hash value.
		/// </summary>
		protected uint CurrentHashValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FNV1a"/> class.
		/// </summary>
		public FNV1a()
		{
			this.HashSizeValue = 32;
			this.Initialize();
		}

		/// <summary>
		/// Initializes an instance of <see cref="T:FNV1a"/>.
		/// </summary>
		public override void Initialize()
		{
			this.CurrentHashValue = Offset;
		}

		/// <summary>Routes data written to the object into the <see cref="T:FNV1a" /> hash algorithm for computing the hash.</summary>
		/// <param name="array">The input data. </param>
		/// <param name="ibStart">The offset into the byte array from which to begin using data. </param>
		/// <param name="cbSize">The number of bytes in the array to use as data. </param>
		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			int end = ibStart + cbSize;

			for (int i = ibStart; i < end; i++)
			{
				this.CurrentHashValue = (this.CurrentHashValue ^ array[i]) * FNV1a.Prime;
			}
		}

		/// <summary>
		/// Returns the computed <see cref="T:FNV1a" /> hash value after all data has been written to the object.
		/// </summary>
		/// <returns>The computed hash code.</returns>
		protected override byte[] HashFinal()
		{
			return BitConverter.GetBytes(this.CurrentHashValue);
		}
	}

	/// <summary>
	/// Implements a modified FNV hash. Provides better distribution than FNV1 but it's only 32 bit long.
	/// </summary>
	/// <remarks>Algorithm found at http://bretm.home.comcast.net/hash/6.html</remarks>
	public class ModifiedFNV : FNV1a
	{
		/// <summary>
		/// Returns the computed <see cref="T:ModifiedFNV" /> hash value after all data has been written to the object.
		/// </summary>
		/// <returns>The computed hash code.</returns>
		protected override byte[] HashFinal()
		{
			this.CurrentHashValue += this.CurrentHashValue << 13;
			this.CurrentHashValue ^= this.CurrentHashValue >> 7;
			this.CurrentHashValue += this.CurrentHashValue << 3;
			this.CurrentHashValue ^= this.CurrentHashValue >> 17;
			this.CurrentHashValue += this.CurrentHashValue << 5;

			return base.HashFinal();
		}
	}
}